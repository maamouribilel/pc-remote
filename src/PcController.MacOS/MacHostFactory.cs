using System.Net;
using PcController.Core;
using PcController.Host;
namespace PcController.MacOS;

public sealed class MacHostFactory
{
    public string DirectoryPath { get; }
    public MacInput Input { get; }
    public MacStorage Storage { get; }
    public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "PC Remote");
    public MacHostFactory(string? directory = null)
    {
        MacNative.RequireUser(); MacNative.umask(0x3f); // 077; preferences/public exports also stay per-user.
        DirectoryPath = directory ?? DefaultDirectory;
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
            // Capabilities and readiness follow the Accessibility permission only. Secure Input,
            // the login screen and an inactive session are transient: native posting rechecks
            // them before every event and the shell pauses, so they never rebuild the host.
            var ready = MacPermissions.Accessibility;
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
    private ulong generation, networkGeneration;
    private bool ready, permission;
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
        generation = MacNative.pc_generation(); networkGeneration = MacNative.pc_network_generation();
    }
    // Synchronous check for the unsafe callback: did a network-trust event occur since the last Inspect?
    public bool NetworkChangedSinceInspect { get { lock (gate) return MacNative.pc_network_generation() != networkGeneration; } }
    // Changed: pause and release (any lifecycle generation, permission or session transition).
    // CapabilitiesChanged: only the Accessibility permission flipped, so the host's capability
    // snapshot must be rebuilt. PermissionReady distinguishes a missing permission (onboarding)
    // from a transient Secure Input/login/inactive-session block (wait, then Resume).
    public (bool Changed, bool NetworkChanged, bool Ready, bool StorageReady, ulong Revision, string Message, bool PermissionReady, bool CapabilitiesChanged) Inspect()
    {
        lock (gate)
        {
            var current = MacNative.pc_generation(); var changed = current != generation; generation = current;
            var network = MacNative.pc_network_generation(); var networkChanged = network != networkGeneration; networkGeneration = network;
            var granted = MacPermissions.Accessibility;
            var session = granted ? MacPermissions.SessionState : -1;
            var available = granted && session == 0;
            string message; var storageReady = true;
            try { factory.Storage.Probe(); message = available ? "macOS input ready. Choose Resume after reviewing the target application." : granted ? MacPermissions.SessionBlocked(session, MacPermissions.FocusStatus) : MacPermissions.Recovery; }
            catch (IOException e) { available = false; storageReady = false; message = e.Message; }
            changed |= ready != available; ready = available;
            var capabilitiesChanged = permission != granted; permission = granted;
            return (changed, networkChanged, available, storageReady, current, message, granted, capabilitiesChanged);
        }
    }
    public void Arm(ulong inspectedRevision)
    { if (MacNative.pc_arm(inspectedRevision) == 0) throw new InputFailureException("Environment changed during Resume. Review the current network and retry."); }
    public void ReleaseInput() => factory.Input.ReleaseAll();
    public void Dispose() { MacNative.pc_unobserve(); GC.KeepAlive(callback); }
}
