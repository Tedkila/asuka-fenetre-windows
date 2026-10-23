using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace Asuka.Windows;

// OpenCvSharp exposes YuNet but not FaceRecognizerSF. Use OpenCV DNN directly,
// with the OpenCV SFace reference preprocessing: aligned 112x112, RGB, scale 1, mean 0.
public sealed class SFace : IDisposable
{
    private readonly Net net;
    private static readonly Point2d[] Target = [new(38.2946, 51.6963), new(73.5318, 51.5014), new(56.0252, 71.7366), new(41.5493, 92.3655), new(70.7299, 92.2041)];
    public SFace(string path)
    {
        net = CvDnn.ReadNetFromOnnx(path) ?? throw new InvalidDataException("Modèle SFace illisible.");
        net.SetPreferableBackend(Backend.OPENCV);
        net.SetPreferableTarget(TargetDevice);
    }
    private static OpenCvSharp.Dnn.Target TargetDevice => OpenCvSharp.Dnn.Target.CPU;
    public void AlignCrop(Mat image, Mat faces, Mat aligned)
    {
        var source = new Point2d[5];
        for (int i = 0; i < 5; i++) source[i] = new(faces.At<float>(0, 4 + i * 2), faces.At<float>(0, 5 + i * 2));
        // Least-squares similarity (uniform scale + rotation + translation), no reflection.
        double sx = source.Average(p => p.X), sy = source.Average(p => p.Y);
        double tx = Target.Average(p => p.X), ty = Target.Average(p => p.Y);
        double den = 0, a = 0, b = 0;
        for (int i = 0; i < 5; i++)
        {
            double x = source[i].X - sx, y = source[i].Y - sy;
            double u = Target[i].X - tx, v = Target[i].Y - ty;
            den += x * x + y * y; a += x * u + y * v; b += x * v - y * u;
        }
        if (den < 1e-6) throw new InvalidDataException("Repères faciaux invalides.");
        a /= den; b /= den;
        using var transform = new Mat(2, 3, MatType.CV_64F);
        transform.Set(0, 0, a); transform.Set(0, 1, -b); transform.Set(0, 2, tx - a * sx + b * sy);
        transform.Set(1, 0, b); transform.Set(1, 1, a); transform.Set(1, 2, ty - b * sx - a * sy);
        Cv2.WarpAffine(image, aligned, transform, new OpenCvSharp.Size(112, 112));
    }
    public void Feature(Mat aligned, Mat feature)
    {
        using var blob = CvDnn.BlobFromImage(aligned, 1.0, new OpenCvSharp.Size(112, 112), Scalar.All(0), swapRB: true, crop: false);
        net.SetInput(blob);
        using var output = net.Forward();
        if (output.Total() != 128 || output.Type() != MatType.CV_32F) throw new InvalidDataException("Signature SFace inattendue.");
        output.CopyTo(feature);
    }
    public static double Match(Mat left, Mat right)
    {
        double norm = Cv2.Norm(left) * Cv2.Norm(right);
        return norm > 1e-12 ? left.Dot(right) / norm : -1;
    }
    public void Dispose() => net.Dispose();
}
