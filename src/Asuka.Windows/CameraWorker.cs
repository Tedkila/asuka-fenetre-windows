using System.Security.Cryptography;
using System.Text.Json;
using Asuka.Core;
using OpenCvSharp;

namespace Asuka.Windows;

public sealed record Observation(DateTimeOffset At, bool Face, bool Teddy, bool Motion, bool Running, string Status);

// One owner for every native camera/model handle. Images never leave this worker or reach disk.
public sealed class CameraWorker : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Task task;
    private readonly object gate = new();
    private TaskCompletionSource<float[]>? enrollment;
    private Observation latest = new(DateTimeOffset.UtcNow, false, false, false, false, "Webcam : démarrage…");
    public Observation Latest => Volatile.Read(ref latest);
    public CameraWorker(Settings settings, float[]? signature) => task = Task.Run(() => Run(settings, signature));

    public async Task<float[]> EnrollAsync()
    {
        TaskCompletionSource<float[]> request = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            if (enrollment is not null) throw new InvalidOperationException("Enregistrement déjà en cours.");
            enrollment = request;
        }
        try { return await request.Task.WaitAsync(TimeSpan.FromSeconds(30), stop.Token); }
        finally { lock (gate) { if (enrollment == request) enrollment = null; } }
    }
    private static string Model(string name)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "models");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        var item = manifest.RootElement.GetProperty("models").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
        string file = Path.Combine(root, name);
        using var stream = File.OpenRead(file);
        if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(item.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Modèle non vérifié.");
        return file;
    }
    private async Task Run(Settings settings, float[]? signature)
    {
        try
        {
            using var detector = FaceDetectorYN.Create(Model("face_detection_yunet_2023mar.onnx"), "", new OpenCvSharp.Size(640, 480), 0.85f);
            using var recognizer = new SFace(Model("face_recognition_sface_2021dec.onnx"));
            using var reference = new Mat(1, 128, MatType.CV_32F);
            if (signature is not null) for (int i = 0; i < 128; i++) reference.Set(0, i, signature[i]);
            using var camera = new VideoCapture(settings.Camera, VideoCaptureAPIs.DSHOW);
            if (!camera.IsOpened()) throw new IOException("Webcam indisponible.");
            camera.Set(VideoCaptureProperties.FrameWidth, 640);
            camera.Set(VideoCaptureProperties.FrameHeight, 480);
            camera.Set(VideoCaptureProperties.Fps, 3);
            using var frame = new Mat(); using var gray = new Mat(); using var previous = new Mat();
            using var diff = new Mat(); using var faces = new Mat(); using var aligned = new Mat(); using var feature = new Mat();
            using var firstSample = new Mat();
            TaskCompletionSource<float[]>? active = null;
            int samples = 0;
            while (!stop.IsCancellationRequested)
            {
                if (!camera.Read(frame) || frame.Empty()) throw new IOException("Webcam interrompue.");
                // Explicitly bound processing even if the camera ignores the requested resolution.
                Cv2.Resize(frame, frame, new OpenCvSharp.Size(640, 480));
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Resize(gray, gray, new OpenCvSharp.Size(160, 120));
                bool movement = false;
                if (!previous.Empty())
                {
                    Cv2.Absdiff(gray, previous, diff);
                    Cv2.Threshold(diff, diff, 25, 255, ThresholdTypes.Binary);
                    movement = (double)Cv2.CountNonZero(diff) / (160 * 120) > settings.MotionThreshold;
                }
                gray.CopyTo(previous);
                detector.Detect(frame, faces);
                bool one = faces.Rows == 1;
                bool teddy = false;
                TaskCompletionSource<float[]>? request;
                lock (gate) request = enrollment;
                if (request != active) { active = request; samples = 0; }
                if (one)
                {
                    recognizer.AlignCrop(frame, faces, aligned);
                    recognizer.Feature(aligned, feature);
                    if (signature is not null)
                        teddy = SFace.Match(feature, reference) >= settings.Threshold;
                    if (active is not null)
                    {
                        if (samples == 0) { feature.CopyTo(firstSample); samples = 1; }
                        else if (SFace.Match(feature, firstSample) >= 0.50) samples++;
                        else samples = 0;
                        if (samples >= 8)
                        {
                            var result = new float[128];
                            for (int i = 0; i < 128; i++) result[i] = firstSample.At<float>(0, i);
                            active.TrySetResult(result);
                            lock (gate) { if (enrollment == active) enrollment = null; }
                            active = null;
                            samples = 0;
                        }
                    }
                }
                else samples = 0;
                Volatile.Write(ref latest, new(DateTimeOffset.UtcNow, faces.Rows > 0, teddy, movement, true,
                    active is not null ? $"Enregistrement : {samples}/8 — une seule personne face à la caméra" : "● Webcam active — traitement local"));
                await Task.Delay(330, stop.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            Volatile.Write(ref latest, new(DateTimeOffset.UtcNow, false, false, false, false,
                "Webcam ou modèles indisponibles — vérifiez l’autorisation caméra, puis Réessayer webcam dans les réglages."));
        }
        finally
        {
            lock (gate) enrollment?.TrySetCanceled();
            if (signature is not null) Array.Clear(signature);
            Volatile.Write(ref latest, Latest with { Running = false });
        }
    }
    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        await task;
        stop.Dispose();
    }
}
