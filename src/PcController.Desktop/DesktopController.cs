using System.Net.NetworkInformation;
using PcController.Core;
using PcController.Host;

namespace PcController.Desktop;

// UI composition receives only explicit real host factories. Normal Phase 2 startup has
// no secure store/native adapters yet and therefore starts no listener, even on loopback.
public sealed class DesktopController : IAsyncDisposable
{
    public TrustedNetworks Networks { get; }
    private readonly ServerLifecycle? lifecycle;
    private readonly bool observeNetworks;
    private bool disposed;
    private readonly List<ApprovalPrompt> prompts = [];
    private readonly object promptGate = new();
    public CompanionServer? Server => lifecycle?.Server;
    public string Platform { get; }
    public string Message { get; private set; }
    public string Status => disposed ? "Stopped" : Server is not { } s ? "Setup required" : s.Sessions.Paused ? "Control paused" : s.Sessions.OwnerDeviceId == null ? "No active controller" : s.Sessions.ActiveSessionId == null ? "Controller connecting" : "Controller connected";
    public event Action? Changed;
    public event Action<ApprovalPrompt>? ApprovalRequested;
    public DesktopController(string platform, TrustedNetworks networks, Func<NetworkSnapshot, CompanionServer>? factory = null, bool observeNetworks = true)
    {
        Platform = platform; Networks = networks; this.observeNetworks = observeNetworks;
        Message = platform == "macos"
            ? "Setup required: Keychain storage and native Accessibility input are not implemented yet. No HTTPS listener is running."
            : "Setup required: Secret Service/keyring storage and X11/Wayland input are not implemented yet. No HTTPS listener is running.";
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
        await RefreshAsync();
    }
    public async Task RefreshAsync()
    {
        if (disposed) return;
        var before = Networks.Read();
        if (Server is { } s && before.TrustIdentity != s.Network.TrustIdentity) { s.Sessions.Pause(true); DenyPrompts(); }
        if (lifecycle != null) await lifecycle.RefreshAsync();
        Changed?.Invoke();
    }
    public async Task ApproveNetworkAsync(NetworkCandidate candidate)
    {
        if (!Networks.Approve(candidate)) { Message = "Network changed. Refresh and approve the current interface."; Changed?.Invoke(); return; }
        await RefreshAsync();
    }
    public async Task BlockLanAsync() { Networks.Invalidate(); Server?.Sessions.Pause(true); DenyPrompts(); await RefreshAsync(); }
    public void NewPairing() { if (!disposed) { DenyPrompts(); Server?.Pairing.IssueChallenge(); Changed?.Invoke(); } }
    public void PauseResume()
    {
        if (Server is not { } s || disposed) return;
        var pause = !s.Sessions.Paused;
        if (TryReleaseInput()) s.Sessions.Pause(pause);
        Changed?.Invoke();
    }
    public void ReleaseInput() => TryReleaseInput();
    private bool TryReleaseInput()
    {
        try { Server?.Input.ReleaseAll(); Changed?.Invoke(); return true; }
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
    public async ValueTask DisposeAsync()
    {
        if (disposed) return; disposed = true;
        if (observeNetworks) { NetworkChange.NetworkAddressChanged -= NetworkChanged; NetworkChange.NetworkAvailabilityChanged -= AvailabilityChanged; }
        DenyPrompts(); if (lifecycle != null) await lifecycle.DisposeAsync().ConfigureAwait(false); Changed?.Invoke();
    }
}
