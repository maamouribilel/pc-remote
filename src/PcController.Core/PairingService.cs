using System.Security.Cryptography;
using System.Text;

namespace PcController.Core;

public sealed record DeviceRecord(string Id, string Name, string CredentialHash, DateTimeOffset Expires);
public interface IDeviceRepository
{
    DeviceRecord[] List();
    void Add(DeviceRecord device);
    void Revoke(string id);
}
public sealed record PairingChallenge(string Value, DateTimeOffset Expires);
public sealed record PairingRequest(string Ticket, string Name, DateTimeOffset Expires);
public sealed record PairingClaim(string State, string? Credential = null, DeviceRecord? Device = null);

public sealed class PairingService(IDeviceRepository devices, Func<DateTimeOffset>? clock = null)
{
    private readonly object gate = new();
    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.UtcNow;
    private PairingChallenge? challenge;
    private PairingRequest? pending;
    private string state = "none";
    private bool closed;
    private string? credential;
    private DeviceRecord? approved;
    private readonly Queue<DateTimeOffset> attempts = new();
    public event Action? Changed;
    public static string RandomToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool Matches(string left, string right) => CryptographicOperations.FixedTimeEquals(
        SHA256.HashData(Encoding.UTF8.GetBytes(left)), SHA256.HashData(Encoding.UTF8.GetBytes(right)));

    public PairingChallenge IssueChallenge()
    {
        lock (gate)
        {
            if (closed) throw new InvalidOperationException("pair_closed");
            pending = null; credential = null; approved = null; state = "none";
            challenge = new(RandomToken(), Now.AddMinutes(2));
            Changed?.Invoke();
            return challenge;
        }
    }
    public PairingChallenge? CurrentChallenge { get { lock (gate) return challenge is { } c && c.Expires > Now ? c : null; } }
    public string Status { get { lock (gate) return pending is { } p && p.Expires <= Now ? "expired" : state; } }
    public PairingRequest Request(string supplied, string name)
    {
        lock (gate)
        {
            if (closed) throw new InvalidOperationException("pair_closed");
            while (attempts.TryPeek(out var first) && first <= Now.AddMinutes(-1)) attempts.Dequeue();
            if (attempts.Count >= 5) throw new InvalidOperationException("pair_rate_limit");
            attempts.Enqueue(Now);
            if (supplied.Length != 64 || challenge is not { } c || c.Expires <= Now || !Matches(c.Value, supplied))
                throw new InvalidOperationException("pair_expired_or_invalid");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(char.IsControl))
                throw new InvalidOperationException("invalid_device_name");
            challenge = null; // Single-use, consumed before the approval request can run.
            pending = new(RandomToken(), name.Trim(), Now.AddMinutes(2));
            state = "pending";
            Changed?.Invoke();
            return pending;
        }
    }
    public void Decide(string ticket, bool allow)
    {
        lock (gate)
        {
            if (closed || pending is not { } p || !Matches(p.Ticket, ticket) || p.Expires <= Now || state != "pending") return;
            if (allow)
            {
                credential = RandomToken();
                approved = new(Guid.NewGuid().ToString("N"), p.Name, Hash(credential), Now.AddDays(30));
                try { devices.Add(approved); state = "approved"; }
                catch { credential = null; approved = null; state = "storage_failed"; }
            }
            else state = "denied";
            Changed?.Invoke();
        }
    }
    public PairingClaim Claim(string ticket)
    {
        lock (gate)
        {
            if (ticket.Length != 64 || pending is not { } p || !Matches(p.Ticket, ticket)) return new("pair_invalid");
            if (p.Expires <= Now) { credential = null; return new("pair_expired"); }
            if (state != "approved") return new(state);
            if (credential == null || Authenticate(credential) == null) return new("revoked");
            var result = new PairingClaim("approved", credential, approved);
            credential = null; pending = null; state = "claimed";
            Changed?.Invoke();
            return result;
        }
    }
    public void Close()
    {
        lock (gate) { closed = true; challenge = null; credential = null; approved = null; state = "denied"; Changed?.Invoke(); }
    }
    public DeviceRecord? Authenticate(string token)
    {
        if (token.Length != 64 || !token.All(Uri.IsHexDigit)) return null;
        var hashed = Hash(token);
        return devices.List().FirstOrDefault(d => d.Expires > Now && Matches(d.CredentialHash, hashed));
    }
}
