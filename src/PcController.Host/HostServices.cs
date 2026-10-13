using System.Security.Cryptography.X509Certificates;
using PcController.Core;

namespace PcController.Host;

// No default adapter or storage fallback: production composition is platform-owned.
public sealed record HostServices(NetworkSnapshot Network, ILocalCertificates Certificates,
    IDeviceRepository Devices, PointerSettings PointerSettings, MediaSettings MediaSettings,
    ILauncherSettings Launcher, IMouseInput Input, IPcActions PcActions,
    PlatformCapabilities Capabilities, string WebRoot, HostReadiness? Readiness = null);
public sealed record HostReadiness(string Storage, string Input, string Network, string Message);
public interface ILocalCertificates : IDisposable
{
    X509Certificate2 Root { get; }
    X509Certificate2 Server { get; }
    string ProfilePath { get; }
    string Fingerprint { get; }
    bool Renewed { get; }
}
public interface IProtectedStorage
{
    // Must establish platform filesystem protections before any data is read/written.
    void InitializeDirectory(string directory);
    byte[] Read(string path);
    void Write(string path, byte[] clear);
}
public interface ICertificateKeyLoader
{
    X509Certificate2 Load(byte[] pfx);
}
