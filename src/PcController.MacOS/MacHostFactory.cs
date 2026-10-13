using System.Net;
using PcController.Core;
using PcController.Host;
namespace PcController.MacOS;

public sealed class MacHostFactory
{
    public string DirectoryPath { get; }
    public MacInput Input { get; }
    public MacStorage Storage { get; }
    public MacHostFactory(string? directory = null)
    {
        MacNative.RequireUser(); MacNative.umask(0x3f); // 077; preferences/public exports also stay per-user.
        DirectoryPath = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "PC Remote");
        Storage = new MacStorage(DirectoryPath); Input = new MacInput();
    }
    internal MacHostFactory(string directory, MacStorage storage, MacInput input)
    { DirectoryPath = directory; Storage = storage; Input = input; }
    // Adapter presence is separate from live permission and audio/OS availability.
    public static PlatformCapabilities Capabilities(bool ready) => new("macos", ready, ready, ready, ready,
        true, true, new HashSet<string>(), ready ? MediaProfiles.Actions : new Dictionary<string, IReadOnlySet<string>>());
    public CompanionServer Create(NetworkSnapshot network)
    {
        Storage.Probe(); Input.ReleaseAll();
        var certificateDirectory = Path.Combine(DirectoryPath, "certificates");
        if (!File.Exists(Path.Combine(certificateDirectory, "root.protected")) &&
            (File.Exists(Path.Combine(certificateDirectory, "server.protected")) || File.Exists(Path.Combine(certificateDirectory, "pc-remote-root.cer")) || File.Exists(Path.Combine(DirectoryPath, "devices.protected"))))
            throw new IOException("The installation root is missing. Restore matching Keychain/files; retiring/replacing trust must be a deliberate separate action.");
        var certificates = new LocalCertificates(Path.Combine(DirectoryPath, "certificates"), network.PrivateAddresses.Append(IPAddress.Loopback).ToArray(), Storage, new MacCertificateKeys());
        try
        {
            var ready = MacPermissions.Accessibility && MacPermissions.Interactive;
            var server = new CompanionServer(new(network, certificates, new ProtectedDevices(DirectoryPath, Storage),
                new PointerSettings(DirectoryPath), new MediaSettings(DirectoryPath), new LauncherSettings(DirectoryPath, new MacLauncherPlatform()), Input, new MacPcActions(), Capabilities(ready) with { Shortcuts = ready && MacInput.ShortcutsAvailable, PowerActions = ready && MacNative.pc_can_sleep() != 0 ? new HashSet<string> { "sleep" } : new HashSet<string>() },
                Path.Combine(AppContext.BaseDirectory, "wwwroot"), new("ready", ready ? "ready" : "permission-required", network.PrivateAddresses.Length > 0 ? "approved" : "loopback", ready ? "macOS input/media ready. System audio requires a writable output; focus the selected player. Sleep is permission-dependent; restart/shutdown unavailable." : MacPermissions.Recovery)));
            server.Sessions.Pause(true); // Startup/restart/replacement always requires deliberate Resume.
            return server;
        }
        catch { certificates.Dispose(); throw; }
    }

}
// AppKit observers start on the native UI thread. Polling is also used for TCC/Secure Input
// and Keychain state; native posting independently checks permission/session every event.
public sealed class MacEnvironment : IDisposable
{
    private readonly MacHostFactory factory;
    private ulong generation;
    private bool ready;
    private readonly object gate = new();
    private readonly MacNative.UnsafeCallback callback;
    public event Action? UnsafeChanged;
    public MacEnvironment(MacHostFactory factory)
    {
        this.factory = factory;
        callback = () =>
        {
            // Never propagate exceptions across the native notification ABI.
            try { factory.Input.ReleaseAll(); } catch (InputFailureException) { }
            try { UnsafeChanged?.Invoke(); } catch (Exception) { }
        };
        if (MacNative.pc_observe(callback) == 0)
        {
            MacNative.pc_unobserve(); GC.KeepAlive(callback);
            throw new IOException("macOS network/lifecycle observation unavailable. No LAN startup allowed.");
        }
        generation = MacNative.pc_generation();
    }
    public (bool Changed, bool Ready, bool StorageReady, ulong Revision, string Message) Inspect()
    {
        lock (gate)
        {
            var current = MacNative.pc_generation(); var changed = current != generation; generation = current;
            var available = MacPermissions.Accessibility && MacPermissions.Interactive;
            string message; var storageReady = true;
            try { factory.Storage.Probe(); message = available ? "macOS input ready. Choose Resume after reviewing the target application." : MacPermissions.Recovery; }
            catch (IOException e) { available = false; storageReady = false; message = e.Message; }
            changed |= ready != available; ready = available;
            return (changed, available, storageReady, current, message);
        }
    }
    public void Arm(ulong inspectedRevision)
    { if (MacNative.pc_arm(inspectedRevision) == 0) throw new InputFailureException("Environment changed during Resume. Review the current network and retry."); }
    public void ReleaseInput() => factory.Input.ReleaseAll();
    public void Dispose() { MacNative.pc_unobserve(); GC.KeepAlive(callback); }
}
