using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Asuka.Core;

public sealed record Settings
{
    public string Server { get; init; } = "http://192.168.1.26:8765/";
    public string Device { get; init; } = Environment.MachineName;
    public string Room { get; init; } = "Cuisine";
    public int IdleMinutes { get; init; } = 3;
    public double Threshold { get; init; } = 0.50;
    public double MotionThreshold { get; init; } = 0.03;
    public int Camera { get; init; }
    public bool Guest { get; init; }
    public bool AutoStart { get; init; }
    public bool KeepAwakePlugged { get; init; }
    public bool SleepEnabled { get; init; }
    public TimeOnly SleepStart { get; init; } = new(23, 0);
    public TimeOnly SleepEnd { get; init; } = new(7, 0);

    public void Validate()
    {
        if (!LocalAddress.TryParse(Server, out _)) throw new ArgumentException("Adresse requise : http(s):// avec une adresse IPv4 privée du réseau local (ou localhost).");
        if (!double.IsFinite(Threshold) || !double.IsFinite(MotionThreshold) || IdleMinutes is < 1 or > 60 || Threshold is < 0.363 or > 0.95 || MotionThreshold is < 0.001 or > 0.5 || Camera is < 0 or > 10)
            throw new ArgumentException("Réglages numériques hors limites.");
        if (string.IsNullOrWhiteSpace(Device) || Device.Length > 80 || string.IsNullOrWhiteSpace(Room) || Room.Length > 80)
            throw new ArgumentException("Appareil et pièce : entre 1 et 80 caractères.");
        if (SleepEnabled && SleepStart == SleepEnd) throw new ArgumentException("Les heures de début et de fin doivent être différentes.");
    }

    public bool IsSleeping(TimeOnly now) => SleepEnabled && (SleepStart < SleepEnd
        ? now >= SleepStart && now < SleepEnd
        : now >= SleepStart || now < SleepEnd);
}

public static class LocalAddress
{
    public static bool TryParse(string text, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https") ||
            u.UserInfo.Length != 0 || u.Query.Length != 0 || u.Fragment.Length != 0) return false;
        bool local = u.Host == "localhost";
        if (IPAddress.TryParse(u.Host, out var ip))
        {
            var b = ip.GetAddressBytes();
            local = IPAddress.IsLoopback(ip) || b.Length == 4 &&
                (b[0] == 10 || b[0] == 192 && b[1] == 168 || b[0] == 172 && b[1] >= 16 && b[1] <= 31);
        }
        if (!local) return false;
        uri = u;
        return true;
    }
    public static bool SameOrigin(Uri allowed, Uri candidate) => candidate.Scheme == allowed.Scheme &&
        candidate.Host == allowed.Host && candidate.Port == allowed.Port && candidate.UserInfo.Length == 0;
}

public static class PinHash
{
    public static string Create(string pin)
    {
        if (pin.Length is < 6 or > 32 || !pin.All(char.IsAsciiDigit))
            throw new ArgumentException("Choisissez un code de 6 à 32 chiffres.");
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, 600_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }
    public static bool Verify(string pin, string stored)
    {
        try
        {
            var parts = stored.Split(':');
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(pin, Convert.FromBase64String(parts[0]), 600_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(parts[1]));
        }
        catch (Exception e) when (e is FormatException or IndexOutOfRangeException or ArgumentException) { return false; }
    }
}
