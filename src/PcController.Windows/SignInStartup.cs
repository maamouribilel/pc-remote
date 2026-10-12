using Microsoft.Win32;

namespace PcController.Windows;

public static class SignInStartup
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "PcController";
    public static bool Enabled { get { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue(ValueName) is string; } }
    public static void SetEnabled(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled)
        {
            if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) || executable.Contains('"')) throw new ArgumentException("An installed executable is required.");
            key.SetValue(ValueName, "\"" + executable + "\" --background", RegistryValueKind.String);
        }
        else key.DeleteValue(ValueName, false);
    }
}
