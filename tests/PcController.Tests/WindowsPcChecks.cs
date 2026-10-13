using PcController.Core;
using PcController.Windows;
internal static class WindowsPcChecks
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
    internal static void Launcher(Action<bool,string> check)
    {
        bool Reject(Action action) { try { action(); return false; } catch (Exception exception) when (exception is InvalidOperationException or ArgumentException) { return true; } }
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
