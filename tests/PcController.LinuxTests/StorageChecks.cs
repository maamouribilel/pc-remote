using System.Net;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using PcController.Core;
using PcController.Host;
using PcController.Linux;
namespace PcController.LinuxTests;
internal static class StorageChecks
{
    internal static async Task Run()
    {
        // Script supplies an isolated HOME, encrypted real GNOME login keyring and private bus.
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        LinuxNative.RequireUser();
        var directory = Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".local/share/pc-remote-test");
        if (!LinuxHostFactory.DefaultDirectory.StartsWith(Environment.GetEnvironmentVariable("HOME")! + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new IOException("Instance check requires the script's isolated HOME.");
        var instance = LinuxSingleInstance.TryAcquire(); Program.Check(instance != null, "real per-user instance lock acquired");
        using (var duplicate = LinuxSingleInstance.TryAcquire()) Program.Check(duplicate == null, "concurrent Linux instance refused");
        instance!.Dispose();
        using (var replacement = LinuxSingleInstance.TryAcquire()) Program.Check(replacement != null, "kernel instance lock released for restart");
        var storage = new LinuxStorage(directory); storage.Probe();
        var path = Path.Combine(directory, "data.protected"); var clear = "disposable storage test"u8.ToArray();
        storage.Write(path, clear); Program.Check(storage.Read(path).SequenceEqual(clear), "real GNOME keyring encrypted envelope");
        Program.Check(File.ReadAllBytes(path).AsSpan().IndexOf(clear) < 0, "ciphertext differs from clear data");
        Program.Check(new LinuxStorage(directory).Read(path).SequenceEqual(clear), "new storage instance retains key");
        Program.Check(File.GetUnixFileMode(directory) == (UnixFileMode)448 && File.GetUnixFileMode(path) == (UnixFileMode)384, "0700/0600 files");
        var copy = Path.Combine(directory, "copy.protected"); File.Copy(path, copy, true);
        try { storage.Read(copy); throw new Exception("wrong AAD accepted"); } catch (IOException) { Program.Check(true, "path authenticated envelope"); } File.Delete(copy);
        var bytes = File.ReadAllBytes(path); bytes[^1] ^= 1; File.WriteAllBytes(path, bytes);
        try { storage.Read(path); throw new Exception("tamper accepted"); } catch (IOException) { Program.Check(true, "tamper fails without identity reset"); } storage.Write(path, clear);
        var symbolic = Path.Combine(directory, "link.protected"); File.CreateSymbolicLink(symbolic, path);
        try { storage.Read(symbolic); throw new Exception("symlink accepted"); } catch (IOException) { Program.Check(true, "symlink refused"); } File.Delete(symbolic);
        var certDir = Path.Combine(directory, "certificates");
        using var first = new LocalCertificates(certDir, [IPAddress.Loopback], storage, new LinuxCertificateKeys());
        var fingerprint = first.Fingerprint; var protectedRoot = File.ReadAllBytes(Path.Combine(certDir, "root.protected"));
        using var renewed = new LocalCertificates(certDir, [IPAddress.Loopback, IPAddress.Parse("192.0.2.10")], storage, new LinuxCertificateKeys());
        Program.Check(renewed.Fingerprint == fingerprint && protectedRoot.SequenceEqual(File.ReadAllBytes(Path.Combine(certDir, "root.protected"))), "SAN renewal retains exact protected root");
        var devices = new ProtectedDevices(directory, storage);
        devices.Add(new("linux-test", "Disposable", PairingService.Hash(new string('a', 64)), DateTimeOffset.UtcNow.AddDays(1)));
        Program.Check(new ProtectedDevices(directory, storage).List().Single().Id == "linux-test", "remembered records persist");
        await LinuxTransport.Run(directory, storage, renewed);
        using var restarted = new LocalCertificates(certDir, [IPAddress.Loopback], storage, new LinuxCertificateKeys());
        Program.Check(restarted.Fingerprint == fingerprint, "certificate restart and address replacement preserve root");
        CryptographicOperations.ZeroMemory(clear);
    }
    internal static void Restart()
    {
        var directory = Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".local/share/pc-remote-test");
        var storage = new LinuxStorage(directory); storage.Probe();
        Program.Check(Encoding.UTF8.GetString(storage.Read(Path.Combine(directory, "data.protected"))) == "disposable storage test", "fresh process retains matching encryption key");
        var devices = new ProtectedDevices(directory, storage);
        var pairing = new PairingService(devices);
        Program.Check(pairing.Authenticate(new string('a', 64))?.Id == "linux-test", "fresh process remembered credential reconnect");
        devices.Revoke("linux-test");
        Program.Check(new PairingService(new ProtectedDevices(directory, storage)).Authenticate(new string('a', 64)) == null, "revocation persisted across new repository");
    }
}
