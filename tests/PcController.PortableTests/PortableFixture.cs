using System.Net;
using System.Security.Cryptography.X509Certificates;
using PcController.Core;
using PcController.Host;

internal static class PortableFixture
{
    internal static string TestExecutable(string directory)
    {
        var path = Path.Combine(directory, "test-player.exe");
        Directory.CreateDirectory(directory); File.WriteAllText(path, "Recording target; never executed."); return path;
    }
    internal static CompanionServer Create(string directory, IMouseInput input, IPcActions? pc = null, NetworkSnapshot? network = null, PlatformCapabilities? capabilities = null)
    {
        network ??= new([], [], "Loopback recording fixture");
        if (network.PrivateAddresses.Any(a => !IPAddress.IsLoopback(a))) throw new InvalidOperationException("Test fixture must remain loopback-only.");
        var data = Path.Combine(directory, "certificates");
        var storage = new TestProtectedStorage();
        var certificates = new LocalCertificates(data, [IPAddress.Loopback], storage, new TestCertificateKeys());
        try
        {
            return new(new(network, certificates, new ProtectedDevices(data, storage), new PointerSettings(data),
                new MediaSettings(data), new LauncherSettings(data, new TestLauncherPlatform(directory)), input, pc ?? new RecordingPcActions(),
                capabilities ?? PlatformCapabilities.ProtocolV1 with { Platform = "recording" }, Path.Combine(AppContext.BaseDirectory, "wwwroot"), new HostReadiness("test-only", "recording", "loopback", "Isolated loopback fixture. Native input is unavailable; commands are recorded only.")));
        }
        catch { certificates.Dispose(); throw; }
    }
}
// Test executable only. Temporary storage is intentionally not a production provider.
// Keeping the historical protected filename exercises file isolation and renewal.
internal sealed class TestProtectedStorage : IProtectedStorage
{
    public void InitializeDirectory(string directory) => Directory.CreateDirectory(directory);
    public byte[] Read(string path) => File.ReadAllBytes(path);
    public void Write(string path, byte[] clear)
    { File.WriteAllBytes(path + ".tmp", clear); File.Move(path + ".tmp", path, true); }
}
internal sealed class TestCertificateKeys : ICertificateKeyLoader
{
    public X509Certificate2 Load(byte[] pfx) => X509CertificateLoader.LoadPkcs12(pfx, null, (OperatingSystem.IsMacOS() ? X509KeyStorageFlags.DefaultKeySet : OperatingSystem.IsWindows() ? X509KeyStorageFlags.UserKeySet : X509KeyStorageFlags.EphemeralKeySet) | X509KeyStorageFlags.Exportable);
}
internal sealed class TestLauncherPlatform(string directory) : ILauncherPlatform
{
    public string Normalize(string path) => Path.GetFullPath(path);
    public StringComparer PathComparer => StringComparer.Ordinal;
    public bool ValidExecutable(string path) => path == Path.Combine(directory, "test-player.exe");
    public bool Available(LauncherApp app) => ValidExecutable(app.Executable) && File.Exists(app.Executable);
    public LauncherApp[] Discover() => [];
}
