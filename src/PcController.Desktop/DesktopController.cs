using System.Net.NetworkInformation;
using PcController.Core;
using PcController.Host;

namespace PcController.Desktop;

// UI composition receives only explicit platform host factories and readiness monitors.
public sealed class DesktopController : IAsyncDisposable
{
    public TrustedNetworks Networks { get; }
    private readonly ServerLifecycle? lifecycle;
    private readonly bool observeNetworks;
    private volatile bool disposed;
    private readonly IDesktopEnvironment? environment;
    private readonly IDesktopStartup? startup;
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    public bool InputReady { get; private set; } = true;
    public bool StorageReady { get; private set; } = true;
    public bool HasStorageAuthorization => environment?.CanAuthorizeStorage == true;
    private int authorizingStorage;
    // Holds no refresh/onboarding gate: the OS dialog may remain open while Exit proceeds.
    public async Task AuthorizeStorageAsync()
    {
        if (environment is not { CanAuthorizeStorage: true } target || disposed || Interlocked.Exchange(ref authorizingStorage, 1) != 0) return;
        var approved = false;
        try
        {
            Message = "Waiting for the macOS Keychain dialog. Choose Always Allow only for this PC Remote.app."; Changed?.Invoke();
            await Task.Run(target.AuthorizeStorage).ConfigureAwait(false);
            approved = true;
        }
        catch (Exception e) when (e is IOException or InvalidOperationException) { Message = e.Message; Changed?.Invoke(); }
        finally { Volatile.Write(ref authorizingStorage, 0); }
        if (approved) await RefreshAsync().ConfigureAwait(false);
    }
    public bool HasPermissionOnboarding => environment != null && environment is not LinuxDesktopEnvironment { NeedsPortalConsent: false };
    public void OpenPermissionSettings()
    {
        if (Platform == "macos") { environment?.Onboard(); return; }
        _ = RequestLinuxPermissionAsync();
    }
    private readonly SemaphoreSlim onboardingGate = new(1, 1);
    private async Task RequestLinuxPermissionAsync()
    {
        if (!await onboardingGate.WaitAsync(0).ConfigureAwait(false)) return;
        try
        {
            Server?.Sessions.Pause(true); DenyPrompts(); Networks.Invalidate();
            await Task.Run(() => environment?.Onboard()).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or InputFailureException) { Message = e.Message; Changed?.Invoke(); }
        finally { onboardingGate.Release(); }
    }
    public bool HasLinuxTextMode => environment is LinuxDesktopEnvironment;
    public void EnableLinuxGtkText(bool enabled)
    {
        Server?.Sessions.Pause(true); DenyPrompts();
        try { (environment as LinuxDesktopEnvironment)?.EnableGtkText(enabled); }
        catch (InputFailureException e) { Message = e.Message; Changed?.Invoke(); }
    }
    public void ConfirmLinuxEndedBackendRecovery()
    {
        Server?.Sessions.Pause(true); DenyPrompts();
        try { (environment as LinuxDesktopEnvironment)?.ConfirmEndedBackendRecovery(); }
        catch (InputFailureException e) { Message = e.Message; Changed?.Invoke(); }
    }
    public void ConfirmLinuxTextRecovery()
    {
        Server?.Sessions.Pause(true); DenyPrompts();
        try { (environment as LinuxDesktopEnvironment)?.ConfirmLocalTextRecovery(); }
        catch (InputFailureException e) { Message = e.Message; Changed?.Invoke(); }
    }
    private readonly List<ApprovalPrompt> prompts = [];
    private readonly object promptGate = new();
    public CompanionServer? Server => lifecycle?.Server;
    public string Platform { get; }
    public string Message { get; private set; }
    public string Status => disposed ? "Stopped" : Server is not { } s ? "Setup required" : s.Sessions.Paused ? "Control paused" : s.Sessions.OwnerDeviceId == null ? "No active controller" : s.Sessions.ActiveSessionId == null ? "Controller connecting" : "Controller connected";
    public event Action? Changed;
    public event Action<ApprovalPrompt>? ApprovalRequested;
    public DesktopController(string platform, TrustedNetworks networks, Func<NetworkSnapshot, CompanionServer>? factory = null, bool observeNetworks = true, IDesktopEnvironment? environment = null, string? setupMessage = null, IDesktopStartup? startup = null)
    {
        this.environment = environment; this.startup = startup;
        if (environment != null) environment.UnsafeChanged += EnvironmentChanged;
        Platform = platform; Networks = networks; this.observeNetworks = observeNetworks;
        Message = platform == "macos"
            ? "Setup required: No platform host is composed: Keychain storage and native Accessibility input services are required. No HTTPS listener is running."
            : "Setup required: No Linux platform host is composed. Reviewed GNOME keyring, X11/portal input and local lifecycle services are required. No HTTPS listener is running.";
        if (setupMessage != null) Message = setupMessage;
        if (factory != null)
        {
            lifecycle = new(factory, networks.Read);
            lifecycle.Updating += () => { DenyPrompts(); Changed?.Invoke(); };
            lifecycle.Ready += s =>
            {
                s.ApproveDeviceAsync = RequestApproval;
                Message = s.Readiness.Message;
                s.Pairing.Changed += Notify; s.Sessions.Changed += Notify;
                Changed?.Invoke();
            };
            lifecycle.Failed += m => { Message = m; Changed?.Invoke(); };
        }
        if (observeNetworks) { NetworkChange.NetworkAddressChanged += NetworkChanged; NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged; }
    }
    private void EnvironmentChanged()
    {
        if (disposed) return;
        // Every unsafe event pauses control. Only network-trust events (macOS: network keys, system
        // sleep/wake, session switch) also revoke LAN approval; Space/display changes keep it.
        if (environment?.NetworkChangedSinceInspect != false) Networks.Invalidate();
        Server?.Sessions.Pause(true); DenyPrompts();
        _ = RefreshAsync(); Changed?.Invoke();
    }
    private void Notify() => Changed?.Invoke();
    private async Task<bool> RequestApproval(PairingRequest request, string address)
    {
        var prompt = new ApprovalPrompt(request, address);
        lock (promptGate) { if (disposed) return false; prompts.Add(prompt); }
        try
        {
            ApprovalRequested?.Invoke(prompt);
            var remaining = request.Expires - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) return false;
            try { return await prompt.Decision.WaitAsync(remaining); }
            catch (TimeoutException) { prompt.Decide(false); return false; }
        }
        finally { lock (promptGate) prompts.Remove(prompt); }
    }
    private void DenyPrompts() { lock (promptGate) foreach (var prompt in prompts) prompt.Decide(false); }
    private void NetworkChanged(object? sender, EventArgs e) => _ = ConnectivityChangedAsync();
    private void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => _ = ConnectivityChangedAsync();
    public async Task ConnectivityChangedAsync()
    {
        Networks.Invalidate(); Server?.Sessions.Pause(true); DenyPrompts();
        await RefreshAsync().ConfigureAwait(false);
    }
    public async Task RefreshAsync()
    {
        await refreshGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed) return;
            var refreshCapabilities = false;
            if (environment != null)
            {
                var state = await Task.Run(environment.Inspect).ConfigureAwait(false);
                InputReady = state.InputReady; StorageReady = state.StorageReady; Message = state.Message; refreshCapabilities = state.Changed;
                if (state.NetworkChanged) Networks.Invalidate();
                if (state.Changed) { Server?.Sessions.Pause(true); DenyPrompts(); }
                if (!state.InputReady) { Server?.Sessions.Pause(true); DenyPrompts(); }
                if (!state.StorageReady)
                { Networks.Invalidate(); if (lifecycle != null) await lifecycle.StopAsync().ConfigureAwait(false); Changed?.Invoke(); return; }
            }
            var before = Networks.Read();
            if (Server is { } s && before.TrustIdentity != s.Network.TrustIdentity) { s.Sessions.Pause(true); DenyPrompts(); }
            if (lifecycle != null) await lifecycle.RefreshAsync(refreshCapabilities).ConfigureAwait(false);
            Changed?.Invoke();
        }
        finally { refreshGate.Release(); }
    }
    public async Task ApproveNetworkAsync(NetworkCandidate candidate)
    {
        if (!Networks.Approve(candidate)) { Message = "Network changed. Refresh and approve the current interface."; Changed?.Invoke(); return; }
        await RefreshAsync().ConfigureAwait(false);
    }
    public async Task BlockLanAsync() { Networks.Invalidate(); Server?.Sessions.Pause(true); DenyPrompts(); await RefreshAsync().ConfigureAwait(false); }
    public void NewPairing() { if (!disposed) { DenyPrompts(); Server?.Pairing.IssueChallenge(); Changed?.Invoke(); } }
    public void PauseResume()
    {
        if (Server is not { } s || disposed) return;
        var pause = !s.Sessions.Paused;
        DesktopEnvironmentState? inspected = null;
        if (!pause && environment != null)
        {
            var state = environment.Inspect(); inspected = state; InputReady = state.InputReady;
            if (!state.InputReady || !state.StorageReady || state.Changed)
            { if (state.NetworkChanged || !state.StorageReady) Networks.Invalidate(); s.Sessions.Pause(true); Message = state.Message; _ = RefreshAsync(); Changed?.Invoke(); return; }
        }
        if (TryReleaseInput())
        {
            try { if (!pause) environment?.Arm(inspected!); s.Sessions.Pause(pause); }
            catch (InputFailureException e) { s.Sessions.Pause(true); Message = e.Message; _ = RefreshAsync(); }
        }
        Changed?.Invoke();
    }
    public void ReleaseInput() => TryReleaseInput();
    private bool TryReleaseInput()
    {
        try { Server?.Input.ReleaseAll(); environment?.ReleaseInput(); Changed?.Invoke(); return true; }
        catch (InputFailureException) { Server?.Sessions.Pause(true); Message = "Input cleanup failed. Control remains paused."; Changed?.Invoke(); return false; }
    }
    public void Revoke(string id)
    {
        try { Server?.RevokeDevice(id); Message = "Device revoked."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Message = "Revocation could not be stored. Control remains paused; unlock the secure store and retry."; }
        Changed?.Invoke();
    }
    public void SavePreferences(PointerPreferences pointer, string media)
    {
        if (Server is not { } s) return;
        if (!TryReleaseInput()) return;
        try { s.PointerSettings.Save(pointer); s.Input.Configure(pointer); s.MediaSettings.Save(media); Message = "Preferences saved."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Message = "Preferences could not be saved."; }
        Changed?.Invoke();
    }
    public bool HasStartup => startup != null;
    public bool StartupEnabled { get { try { return startup?.Enabled == true; } catch (IOException) { return false; } } }
    public void SetStartup(bool enabled)
    {
        try { startup?.Set(enabled); Message = startup?.Message ?? "Startup integration unavailable."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Message = e.Message; }
        Changed?.Invoke();
    }
    public void AddLauncher(string path, string name, string profile) => LocalFeature(s => s.Launcher.Add(path, name, profile));
    public void RemoveLauncher(string id) => LocalFeature(s => s.Launcher.Remove(id));
    public void SetVolume(int level) => LocalFeature(s => { if (!s.Capabilities.Volume || !s.PcActions.SetVolume(level).Available) throw new IOException("System output unavailable or permission lost."); });
    public void SchedulePower(string action) => LocalFeature(s => { if (s.Sessions.Paused) throw new IOException("Resume control before scheduling power."); s.ScheduleLocalPower(action); });
    public void CancelPower() { Server?.CancelPower(); Changed?.Invoke(); }
    private void LocalFeature(Action<CompanionServer> action)
    {
        if (Server is not { } s || !TryReleaseInput()) return;
        try { action(s); Message = "Local preference/action accepted. Check the actual application or output."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException) { Message = e.Message; }
        Changed?.Invoke();
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        Server?.Sessions.Pause(true);
        if (!TryReleaseInput()) throw new InputFailureException("Exit cleanup failed. Restore platform permission and the original session, choose Release input, then retry Exit.");
        environment?.CancelOnboarding();
        await onboardingGate.WaitAsync().ConfigureAwait(false);
        disposed = true;
        if (observeNetworks) { NetworkChange.NetworkAddressChanged -= NetworkChanged; NetworkChange.NetworkAvailabilityChanged -= AvailabilityChanged; }
        DenyPrompts();
        await refreshGate.WaitAsync().ConfigureAwait(false);
        try { if (lifecycle != null) await lifecycle.DisposeAsync().ConfigureAwait(false); if (environment != null) { environment.UnsafeChanged -= EnvironmentChanged; environment.Dispose(); } }
        finally { refreshGate.Release(); }
        onboardingGate.Release();
        Changed?.Invoke();
    }
}
