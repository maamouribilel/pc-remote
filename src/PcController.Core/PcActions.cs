namespace PcController.Core;

public sealed record LauncherApp(string Id, string Name, string Executable, string Profile);
public sealed record VolumeState(bool Available, int Level = 0, bool Muted = false);
public sealed record LaunchResult(bool Focused);
public interface IVolumeActions
{
    VolumeState GetVolume();
    VolumeState SetVolume(int level);
}
public interface ILauncherActions { LaunchResult Launch(LauncherApp app); }
public interface IPowerActions { void Power(string action); }
public interface IPcActions : IVolumeActions, ILauncherActions, IPowerActions { }

public sealed record PowerPreparation(string Token, string Action, string Device, string Session, DateTimeOffset Expires);
public sealed record PendingPower(string Action, string Device, string Session, DateTimeOffset ExecuteAt);

// Only confirmation schedules an action. Every ticket belongs to one live controller.
public sealed class PowerQueue(Func<DateTimeOffset>? clock = null)
{
    private readonly object gate = new();
    private PowerPreparation? preparation;
    private PendingPower? pending;
    private DateTimeOffset Now => clock?.Invoke() ?? DateTimeOffset.UtcNow;
    public PendingPower? Pending { get { lock (gate) return pending; } }
    public static bool Supported(string action) => action is "sleep" or "restart" or "shutdown";
    public PowerPreparation Prepare(string action, string device, string session)
    {
        if (!Supported(action)) throw new ArgumentOutOfRangeException(nameof(action));
        lock (gate)
        {
            if (pending != null) throw new InvalidOperationException("power_pending");
            return preparation = new(PairingService.RandomToken(), action, device, session, Now.AddSeconds(60));
        }
    }
    public PendingPower Confirm(string token, string device, string session)
    {
        lock (gate)
        {
            if (preparation is not { } value || value.Token != token || value.Device != device || value.Session != session || value.Expires <= Now)
                throw new InvalidOperationException("confirmation_expired");
            preparation = null;
            return pending = new(value.Action, device, session, Now.AddSeconds(30));
        }
    }
    public void Cancel() { lock (gate) { preparation = null; pending = null; } }
    public void KeepSession(string? session)
    {
        lock (gate)
            if ((preparation != null && preparation.Session != "local" && preparation.Session != session) || (pending != null && pending.Session != "local" && pending.Session != session))
            { preparation = null; pending = null; }
    }
    public PendingPower? TakeDue(string device, string session)
    {
        lock (gate)
        {
            if (pending is not { } value || value.Device != device || value.Session != session || value.ExecuteAt > Now) return null;
            pending = null; return value;
        }
    }
}
