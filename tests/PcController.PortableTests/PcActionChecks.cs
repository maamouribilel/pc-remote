using PcController.Core;

internal static class PcActionChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var power = new PowerQueue(() => now);
        bool Reject(Action action) { try { action(); return false; } catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return true; } }
        check(Reject(() => power.Prepare("hibernate","phone","session")),"power action allowlist rejects unsupported commands");
        var prepared = power.Prepare("restart","phone","session");
        check(power.Pending == null,"choosing a power action does not schedule it");
        check(Reject(() => power.Confirm("wrong","phone","session")),"power requires the issued confirmation ticket");
        check(Reject(() => power.Confirm(prepared.Token,"other","session")),"confirmation is bound to the paired device");
        check(Reject(() => power.Confirm(prepared.Token,"phone","new-session")),"confirmation is bound to the connected session");
        var pending = power.Confirm(prepared.Token,"phone","session");
        check(pending.ExecuteAt == now.AddSeconds(30),"power confirmation starts a thirty-second countdown");
        check(Reject(() => power.Confirm(prepared.Token,"phone","session")),"power confirmation cannot replay");
        check(power.TakeDue("phone","session") == null,"power action cannot execute before its countdown");
        now = now.AddSeconds(30);
        check(power.TakeDue("phone","session")?.Action == "restart" && power.TakeDue("phone","session") == null,"due power action can execute exactly once");
        prepared = power.Prepare("sleep","phone","session"); now = now.AddSeconds(60);
        check(Reject(() => power.Confirm(prepared.Token,"phone","session")),"expired confirmation does not schedule power");
        foreach (var replacement in new string?[] { null, "takeover" })
        {
            prepared = power.Prepare("shutdown","phone","session"); power.Confirm(prepared.Token,"phone","session"); power.KeepSession(replacement);
            check(power.Pending == null,"disconnect or takeover cancels scheduled power");
        }
        prepared = power.Prepare("sleep","phone","session"); power.Confirm(prepared.Token,"phone","session"); power.Cancel(); now = now.AddMinutes(1);
        check(power.TakeDue("phone","session") == null,"cancel prevents the scheduled action after its original deadline");
        var sessions = new SessionOwnership(); var reservation = sessions.Reserve("phone",false); var executions = 0;
        check(!sessions.ExecuteDevice("phone",reservation.Session.Id,() => executions++),"HTTP PC actions require a connected controller");
        sessions.Connect("phone",reservation.Ticket);
        check(!sessions.ExecuteDevice("other",reservation.Session.Id,() => executions++) && !sessions.ExecuteDevice("phone","stale",() => executions++),"HTTP PC actions reject other devices and stale session IDs");
        check(sessions.ExecuteDevice("phone",reservation.Session.Id,() => executions++) && executions == 1,"active controller can execute a PC action atomically");
        sessions.Pause(true);
        check(!sessions.ExecuteDevice("phone",reservation.Session.Id,() => executions++),"pause blocks PC actions");
    }
}
