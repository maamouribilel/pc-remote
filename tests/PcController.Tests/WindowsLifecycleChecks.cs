using Microsoft.Win32;
using PcController.Windows;
internal static class WindowsLifecycleChecks
{
    internal static void Local(Action<bool, string> check)
    {
        check(!FirewallRules.Exists(Path.Combine(Path.GetTempPath(), "PcRemote-absent-" + Guid.NewGuid().ToString("N") + ".exe")),
            "real Windows firewall absent-rule lookup is safe without elevation");
        var suffix = Guid.NewGuid().ToString("N");
        using var primary = new UserInstance(suffix);
        using var secondary = new UserInstance(suffix);
        check(primary.IsPrimary && !secondary.IsPrimary, "single same-user instance enforced");
        var actions = new List<LocalAction>();
        var listener = primary.ListenAsync(action => { actions.Add(action); return Task.CompletedTask; });
        foreach (var action in Enum.GetValues<LocalAction>())
            check(secondary.SendAsync(action).GetAwaiter().GetResult(), "same-user local IPC acknowledgement: " + action);
        check(!secondary.SendAsync((LocalAction)255).GetAwaiter().GetResult() && actions.SequenceEqual(Enum.GetValues<LocalAction>()), "IPC accepts only declared local actions");
        primary.Dispose(); listener.GetAwaiter().GetResult();
        // Preserve the user's preference, even if a verification check fails.
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        var original = key.GetValue(SignInStartup.ValueName);
        var kind = original == null ? RegistryValueKind.String : key.GetValueKind(SignInStartup.ValueName);
        try
        {
            SignInStartup.SetEnabled(true, Environment.ProcessPath!);
            check(SignInStartup.Enabled && (string)key.GetValue(SignInStartup.ValueName)! == "\"" + Environment.ProcessPath + "\" --background", "current-user sign-in executable quoted with background option");
            SignInStartup.SetEnabled(false, Environment.ProcessPath!);
            check(!SignInStartup.Enabled, "optional sign-in preference can be removed");
        }
        finally { if (original == null) key.DeleteValue(SignInStartup.ValueName, false); else key.SetValue(SignInStartup.ValueName, original, kind); }
    }
}
