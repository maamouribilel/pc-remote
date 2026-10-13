using System.Security.Cryptography;
using System.Text;

namespace PcController.Core;

public sealed class ControlSession(string deviceId, string ticket, DateTimeOffset expires)
{
    public string DeviceId { get; } = deviceId;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public CancellationTokenSource Stop { get; } = new();
    internal string TicketHash { get; } = PairingService.Hash(ticket);
    internal DateTimeOffset Expires { get; } = expires;
    internal bool Connected { get; set; }
}
public sealed class SessionOwnership(Func<DateTimeOffset>? clock = null)
{
    private readonly object gate = new();
    private ControlSession? owner;
    private bool paused;
    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.UtcNow;
    public event Action? Changed;
    public event Action? ReleaseInput;
    public bool Paused { get { lock (gate) return paused; } }
    public string? ActiveSessionId { get { lock (gate) return !paused && owner is { Connected: true } ? owner.Id : null; } }
    public string? OwnerDeviceId { get { lock (gate) { ExpireReservation(); return owner?.DeviceId; } } }
    public (ControlSession Session, string Ticket) Reserve(string device, bool takeover)
    {
        lock (gate)
        {
            ExpireReservation();
            if (paused) throw new InvalidOperationException("paused");
            if (owner != null && !takeover) throw new InvalidOperationException("controller_busy");
            ClearOwner(); // Releases input before assigning a replacement.
            if (paused) throw new InvalidOperationException("paused");
            var ticket = PairingService.RandomToken();
            owner = new(device, ticket, Now.AddSeconds(30));
            Changed?.Invoke();
            return (owner, ticket);
        }
    }
    public (ControlSession Session, string Ticket)? ReserveAuthorized(string device, bool takeover, Func<bool> authenticate)
    {
        lock (gate) { return authenticate() ? Reserve(device, takeover) : null; }
    }
    public ControlSession? Connect(string device, string ticket)
    {
        lock (gate)
        {
            ExpireReservation();
            if (paused || owner == null || owner.Connected || owner.DeviceId != device ||
                ticket.Length != 64 || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(owner.TicketHash), Encoding.ASCII.GetBytes(PairingService.Hash(ticket)))) return null;
            owner.Connected = true;
            Changed?.Invoke();
            return owner;
        }
    }
    public bool Execute(ControlSession session, Action action)
    {
        lock (gate)
        {
            if (paused || owner != session || !session.Connected || session.Stop.IsCancellationRequested) return false;
            action(); return true;
        }
    }
    public bool ExecuteDevice(string device, string session, Action action)
    {
        lock (gate)
        {
            if (paused || owner is not { Connected: true } current || current.DeviceId != device || current.Id != session || current.Stop.IsCancellationRequested) return false;
            action(); return true;
        }
    }
    public void End(ControlSession session) { lock (gate) { if (owner == session) ClearOwner(); } }
    public void EndCurrent() { lock (gate) ClearOwner(); }
    public void Revoke(string device) { lock (gate) { if (owner?.DeviceId == device) ClearOwner(); } }
    public void Pause(bool value) { lock (gate) { paused = value; if (value) ClearOwner(); Changed?.Invoke(); } }
    public void Shutdown() { lock (gate) { paused = true; ClearOwner(); } }
    private void ExpireReservation() { if (owner is { Connected: false } current && current.Expires <= Now) ClearOwner(); }
    private void ClearOwner()
    {
        if (owner == null) return;
        var previous = owner; owner = null;
        try { ReleaseInput?.Invoke(); }
        catch (InputFailureException) { paused = true; } // Fail closed if the adapter cannot release input.
        finally { previous.Stop.Cancel(); }
        Changed?.Invoke();
    }
}
