using PcController.MacOS;
using PcController.Linux;
namespace PcController.Desktop;

public sealed record DesktopEnvironmentState(bool Changed, bool InputReady, bool StorageReady, string Message, ulong Revision = 0);
public interface IDesktopEnvironment : IDisposable
{
    event Action? UnsafeChanged;
    DesktopEnvironmentState Inspect();
    void Onboard();
    void CancelOnboarding() { }
    void Arm(DesktopEnvironmentState inspected);
    void ReleaseInput();
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
        return new(state.Changed, state.Ready, state.StorageReady, state.Message, state.Revision);
    }
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
