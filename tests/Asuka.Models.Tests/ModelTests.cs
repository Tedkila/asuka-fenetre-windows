using System.Security.Cryptography;
using System.Text.Json;
using Asuka.Windows;
using OpenCvSharp;
using Xunit;
namespace Asuka.Models.Tests;

public class ModelTests
{
    [Fact] public void BothModelsAreVerifiedAndRunOnCpu()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "models");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        foreach (var model in manifest.RootElement.GetProperty("models").EnumerateArray())
        {
            using var input = File.OpenRead(Path.Combine(root, model.GetProperty("name").GetString()!));
            Assert.Equal(model.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant());
        }
        using var detector = FaceDetectorYN.Create(Path.Combine(root, "face_detection_yunet_2023mar.onnx"), "", new OpenCvSharp.Size(640, 480));
        using var black = new Mat(480, 640, MatType.CV_8UC3, Scalar.All(0));
        using var detections = new Mat();
        detector.Detect(black, detections);
        Assert.Equal(0, detections.Rows);
        using var recognizer = new SFace(Path.Combine(root, "face_recognition_sface_2021dec.onnx"));
        using var aligned = new Mat(112, 112, MatType.CV_8UC3, Scalar.All(128));
        using var feature = new Mat();
        recognizer.Feature(aligned, feature);
        Assert.Equal(128, feature.Total());
        Assert.Equal(1, feature.Rows); Assert.Equal(128, feature.Cols);
        for (int i = 0; i < 128; i++) Assert.True(float.IsFinite(feature.At<float>(0, i)));
        Assert.InRange(SFace.Match(feature, feature), 0.9999, 1.0001);
    }
    [Fact] public void CanonicalLandmarksPreserveTheAlignedImage()
    {
        using var recognizer = new SFace(Path.Combine(AppContext.BaseDirectory, "models", "face_recognition_sface_2021dec.onnx"));
        using var image = new Mat(112, 112, MatType.CV_8UC3);
        Cv2.Randu(image, Scalar.All(0), Scalar.All(255));
        using var faces = new Mat(1, 15, MatType.CV_32F, Scalar.All(0));
        float[] landmarks = [38.2946f, 51.6963f, 73.5318f, 51.5014f, 56.0252f, 71.7366f, 41.5493f, 92.3655f, 70.7299f, 92.2041f];
        for (int i = 0; i < landmarks.Length; i++) faces.Set(0, i + 4, landmarks[i]);
        using var aligned = new Mat();
        recognizer.AlignCrop(image, faces, aligned);
        Assert.Equal(112, aligned.Width); Assert.Equal(112, aligned.Height);
        Assert.InRange(Cv2.Norm(image, aligned, NormTypes.INF), 0, 1);
    }
}
