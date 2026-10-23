using System.Security.Cryptography;
using System.Text.Json;
using Asuka.Windows;
using OpenCvSharp;
using Xunit;
namespace Asuka.Windows.Tests;

public class NativeTests
{
    [Fact] public void WindowsDpapiRoundTripAndFaceDeletion()
    {
        // The CI job must use a disposable Windows user. Never overwrite an existing profile.
        Assert.False(Directory.Exists(Storage.DirectoryPath), "Refuse de modifier un profil utilisateur existant.");
        try
        {
            var store = new Storage();
            var face = Enumerable.Range(0, 128).Select(i => (float)i / 128).ToArray();
            var data = new UserData { Pin = Asuka.Core.PinHash.Create("987654"), Token = "CI-fake-token", Face = face };
            store.Save(data);
            Assert.Equal(face, store.Load().Face);
            Assert.Equal(data.Token, store.Load().Token);
            byte[] protectedBytes = File.ReadAllBytes(Path.Combine(Storage.DirectoryPath, "profil.dpapi"));
            Assert.DoesNotContain("CI-fake-token", System.Text.Encoding.UTF8.GetString(protectedBytes));
            store.Save(data with { Face = null });
            Assert.Null(store.Load().Face);
            Assert.Single(Directory.GetFiles(Storage.DirectoryPath));
        }
        finally { if (Directory.Exists(Storage.DirectoryPath)) Directory.Delete(Storage.DirectoryPath, true); }
    }
}
