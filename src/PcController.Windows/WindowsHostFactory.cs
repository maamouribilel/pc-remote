using System.Net;
using PcController.Core;
using PcController.Host;
namespace PcController.Windows;

public sealed class WindowsTrustedNetworkDiscovery : ITrustedNetworkDiscovery
{
    public NetworkSnapshot Read() => WindowsNetworkDiscovery.Read();
}
public static class WindowsHostFactory
{
    public static CompanionServer Create(NetworkSnapshot network, WindowsInput nativeInput)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcController", "certificates");
        var certificates = new LocalCertificates(directory, network.PrivateAddresses.Append(IPAddress.Loopback).ToArray());
        try
        {
            return new CompanionServer(new(network, certificates, new ProtectedDevices(directory),
                new PointerSettings(directory), new MediaSettings(directory), new LauncherSettings(directory),
                nativeInput, new WindowsPcActions(), PlatformCapabilities.ProtocolV1 with { Platform = "windows" }, Path.Combine(AppContext.BaseDirectory, "wwwroot")));
        }
        catch { certificates.Dispose(); throw; }
    }
}
