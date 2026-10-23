using System.Security.Cryptography;
using System.Text.Json;
using Asuka.Core;
using Microsoft.Win32;

namespace Asuka.Windows;

public sealed record UserData
{
    public Settings Settings { get; init; } = new();
    public string Pin { get; init; } = "";
    public string Token { get; init; } = "";
    public float[]? Face { get; init; }
}

public sealed class Storage
{
    public static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FenetreAsuka");
    private readonly string path = Path.Combine(DirectoryPath, "profil.dpapi");
    public UserData Load()
    {
        if (!File.Exists(path)) return new();
        byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        try
        {
            var data = JsonSerializer.Deserialize<UserData>(plain) ?? throw new InvalidDataException();
            data.Settings.Validate();
            if (data.Face is not null && (data.Face.Length != 128 || data.Face.Any(x => !float.IsFinite(x))))
                throw new InvalidDataException("Signature invalide.");
            return data;
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    public void Save(UserData data)
    {
        data.Settings.Validate();
        Directory.CreateDirectory(DirectoryPath);
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(data);
        try
        {
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            string temp = path + ".tmp";
            File.WriteAllBytes(temp, encrypted);
            File.Move(temp, path, true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    public static void AutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("FenetreAsuka", "\"" + Environment.ProcessPath + "\"");
        else key.DeleteValue("FenetreAsuka", false);
    }
}
