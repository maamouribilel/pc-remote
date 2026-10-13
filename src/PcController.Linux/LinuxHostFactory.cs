using System.Net;
using PcController.Core;
using PcController.Host;
namespace PcController.Linux;

public sealed class LinuxHostFactory
{
    public static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "pc-remote");
    public string DirectoryPath { get; }
    public LinuxInput Input { get; }
    public LinuxStorage Storage { get; }
    public LinuxBackend Backend { get; }
    public LinuxHostFactory()
    {
        LinuxNative.RequireUser(); LinuxSession.RequireBaseline(); LinuxNative.umask(0x3f);
        Backend = LinuxSession.Detect(); DirectoryPath = DefaultDirectory;
        Storage = new(DirectoryPath); Input = new();
    }
    public PlatformCapabilities Capabilities(bool ready) => new("linux", ready, ready, ready && Input.GtkTextEnabled,
        ready, true, true, ready ? LinuxPcActions.PowerCapabilities() : new HashSet<string>(), ready ? MediaProfiles.Actions : new Dictionary<string, IReadOnlySet<string>>());
    public CompanionServer Create(NetworkSnapshot network)
    {
        if (LinuxNative.pc_linux_observed() == 0) throw new IOException("Linux network/lifecycle observation unavailable; listeners must stay stopped.");
        Storage.Probe(); Input.ReleaseAll();
        var certificateDirectory = Path.Combine(DirectoryPath, "certificates");
        if (!File.Exists(Path.Combine(certificateDirectory, "root.protected")) &&
            (File.Exists(Path.Combine(certificateDirectory, "server.protected")) || File.Exists(Path.Combine(certificateDirectory, "pc-remote-root.cer")) || File.Exists(Path.Combine(DirectoryPath, "devices.protected"))))
            throw new IOException("Installation root missing. Restore matching keyring/files; replacing installation trust requires deliberate recovery.");
        var certificates = new LocalCertificates(certificateDirectory, network.PrivateAddresses.Append(IPAddress.Loopback).ToArray(), Storage, new LinuxCertificateKeys());
        try
        {
            var ready = LinuxNative.pc_linux_ready() != 0;
            var server = new CompanionServer(new(network, certificates, new ProtectedDevices(DirectoryPath, Storage),
                new PointerSettings(DirectoryPath), new MediaSettings(DirectoryPath), new LauncherSettings(DirectoryPath, new LinuxLauncherPlatform()), Input, new LinuxPcActions(), Capabilities(ready),
                Path.Combine(AppContext.BaseDirectory, "wwwroot"), new("ready", ready ? "ready" : "permission-required", network.PrivateAddresses.Length > 0 ? "approved" : "loopback",
                    $"Linux {Backend}: {(ready ? "pointer/navigation candidate ready; Resume deliberately" : "local desktop/portal permission required")}. Text requires explicit GTK target mode. Media uses native MPRIS or focused player shortcuts; audio and logind power are permission-dependent.")));
            server.Sessions.Pause(true); return server;
        }
        catch { certificates.Dispose(); throw; }
    }

}
public sealed class LinuxEnvironment : IDisposable
{
    private readonly LinuxHostFactory factory;
    private readonly LinuxNative.UnsafeCallback callback;
    private ulong generation;
    private bool ready;
    private readonly object gate = new();
    public event Action? UnsafeChanged;
    public LinuxEnvironment(LinuxHostFactory factory)
    {
        this.factory = factory;
        callback = () =>
        {
            try { factory.Input.ReleaseAll(); } catch (InputFailureException) { }
            try { UnsafeChanged?.Invoke(); } catch (Exception) { }
        };
        if (LinuxNative.pc_linux_open((int)factory.Backend, callback) == 0)
        {
            LinuxNative.pc_linux_close(); GC.KeepAlive(callback);
            throw new IOException("Local logind session/session bus observation unavailable. No production Linux listener allowed.");
        }
        generation = LinuxNative.pc_linux_generation();
    }
    public (bool Changed, bool Ready, bool StorageReady, ulong Revision, string Message) Inspect()
    {
        lock (gate)
        {
            var current = LinuxNative.pc_linux_generation(); var changed = generation != current; generation = current;
            var available = LinuxNative.pc_linux_ready() != 0;
            var storageReady = true;
            var message = available ? $"Linux {factory.Backend} input candidate ready. Text is {(factory.Input.GtkTextEnabled ? "GTK Unicode only" : "disabled")}; choose Resume after reviewing the target." : "Input blocked: use the local unlocked GNOME session; on Wayland explicitly request Remote Desktop consent. XWayland is not input permission.";
            try { factory.Storage.Probe(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { storageReady = available = false; message = "GNOME storage unavailable; listeners stopped. " + e.Message; }
            if (LinuxNative.pc_linux_observed() == 0) { available = storageReady = false; message = "Linux network/lifecycle observation unavailable. All listeners stopped until the original services recover."; }
            changed |= ready != available; ready = available;
            return (changed, available, storageReady, current, message);
        }
    }
    public void Onboard()
    {
        if (factory.Backend != LinuxBackend.WaylandPortal) return;
        factory.Input.ReleaseAll(); // Controller pauses/cancels ownership before invoking onboarding.
        if (LinuxNative.pc_linux_consent() == 0) throw new IOException("Remote Desktop portal denied, expired, cancelled, closed or unavailable, or did not grant both pointer and keyboard. Retry locally; no input fallback is used.");
    }
    public void CancelOnboarding() => LinuxNative.pc_linux_cancel_consent();

    public void EnableGtkText(bool enabled)
    {
        factory.Input.WithExclusiveBackend(() => { factory.Input.ReleaseAll(); factory.Input.GtkTextEnabled = enabled; });
        UnsafeChanged?.Invoke(); // Cancels ownership and recreates authenticated capability snapshot.
    }
    public void ConfirmEndedBackendRecovery() { factory.Input.ConfirmEndedBackendRecovery(); UnsafeChanged?.Invoke(); }
    public void ConfirmLocalTextRecovery() { factory.Input.ConfirmLocalTextRecovery(); UnsafeChanged?.Invoke(); }
    public void Arm(ulong revision) { if (LinuxNative.pc_linux_arm(revision) == 0) throw new InputFailureException("Linux environment changed during Resume. Refresh and reapprove the network/session."); }
    public void ReleaseInput() => factory.Input.ReleaseAll();
    public void Dispose() { LinuxNative.pc_linux_close(); GC.KeepAlive(callback); }
}
