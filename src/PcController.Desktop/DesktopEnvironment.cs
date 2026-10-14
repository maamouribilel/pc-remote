using PcController.MacOS;
using PcController.Linux;
namespace PcController.Desktop;

public sealed record DesktopEnvironmentState(bool Changed, bool InputReady, bool StorageReady, string Message, ulong Revision = 0)
{
    // Whether the trusted LAN may have changed. Platforms without the distinction treat every change as one.
    public bool NetworkChanged { get; init; } = Changed;
    // Whether the platform input permission exists. Platforms without the distinction equate it with
    // input readiness; macOS keeps it while Secure Input, the login screen or an inactive session
    // only pauses control, so the shell offers onboarding only for a missing permission.
    public bool PermissionReady { get; init; } = InputReady;
    // Whether the host's capability snapshot must be rebuilt (permission or mode change). Pause-only
    // transitions (Space/display/Secure Input) keep the running host, its pairing link and prompts.
    public bool CapabilitiesChanged { get; init; } = Changed;
}
public interface IDesktopEnvironment : IDisposable
{
    event Action? UnsafeChanged;
    DesktopEnvironmentState Inspect();
    void Onboard();
    void CancelOnboarding() { }
    void Arm(DesktopEnvironmentState inspected);
    void ReleaseInput();
    // Whether an unsafe callback since the last Inspect may have changed the network (default: yes).
    bool NetworkChangedSinceInspect => true;
    // Explicit local OS consent for an existing storage key after an app update; never automatic.
    bool CanAuthorizeStorage => false;
    void AuthorizeStorage() => throw new NotSupportedException();
}
internal sealed class MacDesktopEnvironment(MacHostFactory factory) : IDesktopEnvironment
{
    private readonly MacEnvironment environment = new(factory);
    public event Action? UnsafeChanged { add => environment.UnsafeChanged += value; remove => environment.UnsafeChanged -= value; }
    public DesktopEnvironmentState Inspect()
    {
        var state = environment.Inspect();
        return new(state.Changed, state.Ready, state.StorageReady, state.Message, state.Revision)
        { NetworkChanged = state.NetworkChanged, PermissionReady = state.PermissionReady, CapabilitiesChanged = state.CapabilitiesChanged };
    }
    public bool NetworkChangedSinceInspect => environment.NetworkChangedSinceInspect;
    public void Arm(DesktopEnvironmentState inspected) => environment.Arm(inspected.Revision);
    public void ReleaseInput() => environment.ReleaseInput();
    public void Onboard() => MacPermissions.OpenAccessibilitySettings();
    public bool CanAuthorizeStorage => true;
    public void AuthorizeStorage() => factory.Storage.AuthorizeExistingKey();
    public void Dispose() => environment.Dispose();
}

internal sealed class LinuxDesktopEnvironment(LinuxHostFactory factory) : IDesktopEnvironment
{
    public bool NeedsPortalConsent => factory.Backend == LinuxBackend.WaylandPortal;
    public bool GtkTextEnabled => factory.Input.GtkTextEnabled;
    private readonly LinuxEnvironment environment = new(factory);
    public event Action? UnsafeChanged { add => environment.UnsafeChanged += value; remove => environment.UnsafeChanged -= value; }
    public DesktopEnvironmentState Inspect()
    {
        var state = environment.Inspect();
        return new(state.Changed, state.Ready, state.StorageReady, state.Message, state.Revision);
    }
    public void Arm(DesktopEnvironmentState state) => environment.Arm(state.Revision);
    public void ReleaseInput() => environment.ReleaseInput();
    public void Onboard() => environment.Onboard();
    public void CancelOnboarding() => environment.CancelOnboarding();
    public void EnableGtkText(bool enabled) => environment.EnableGtkText(enabled);
    public void ConfirmEndedBackendRecovery() => environment.ConfirmEndedBackendRecovery();
    public void ConfirmLocalTextRecovery() => environment.ConfirmLocalTextRecovery();
    public void Dispose() => environment.Dispose();
}
