using PcController.Core;
using PcController.Windows;

internal static class PcActionChecks
{
    internal static void Native(Action<bool,string> check)
    {
        var volume = WindowsVolume.Read();
        check(volume.Available,"real Windows default audio endpoint is readable");
        if (!volume.Available) return;
        if (!volume.Muted)
        {
            try
            {
                var requested = Math.Max(0,volume.Level - 1);
                var actual = WindowsVolume.Set(requested);
                check(actual.Available && actual.Level == requested && WindowsVolume.Read().Level == requested,"real Windows volume accepts an exact percentage");
            }
            finally { WindowsVolume.Set(volume.Level); }
            check(WindowsVolume.Read().Level == volume.Level,"native volume smoke restores the original level");
        }
        else Console.WriteLine("SKIP native volume write: Windows was muted; mute state preserved.");
    }
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
        var directory = Path.Combine(Path.GetTempPath(),"PcLauncherTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var launcher = new LauncherSettings(directory);
            foreach (var existing in launcher.List()) launcher.Remove(existing.Id);
            var executable = Path.Combine(Environment.SystemDirectory,"notepad.exe");
            launcher.Add(executable,"Notes","General");
            var entry = launcher.List().Single();
            check(new LauncherSettings(directory).Find(entry.Id)?.Executable == executable,"PC-selected launcher app persists under an opaque ID");
            check(Reject(() => launcher.Add(executable,"Duplicate","General")),"duplicate executable cannot flood launcher");
            check(!LauncherSettings.ValidExecutable(@"\\server\share\app.exe") && !LauncherSettings.ValidExecutable("cmd.exe") && !LauncherSettings.ValidExecutable(Path.Combine(directory,"script.ps1")),"launcher excludes remote paths, relative commands and scripts");
            launcher.Remove(entry.Id); check(new LauncherSettings(directory).List().Length == 0,"removed apps stay removed after restart");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
}
