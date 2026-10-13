using PcController.Core;
namespace PcController.MacOS;

public sealed class MacPcActions : IPcActions
{
    private readonly object gate = new();
    public VolumeState GetVolume() { lock (gate) return Read(-1); }
    private static VolumeState Read(int mode, int level = 0) => MacNative.pc_volume(mode, level, out var actual, out var muted) != 0 ? new(true, actual, muted != 0) : new(false);
    public VolumeState SetVolume(int level)
    {
        if (level is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(level));
        lock (gate) return Read(0, level);
    }
    internal void GeneralVolume(string action)
    {
        lock (gate)
        {
            var current = Read(-1);
            if (!current.Available) throw new InputFailureException("Default output has no writable volume/mute control.");
            var result = action == "mute" ? Read(1) : Read(0, Math.Clamp(current.Level + (action == "volumeUp" ? 5 : -5), 0, 100));
            if (!result.Available) throw new InputFailureException("System audio unavailable.");
        }
    }
    public LaunchResult Launch(LauncherApp app)
    {
        if (!new MacLauncherPlatform().Available(app) || MacNative.pc_launch(app.Executable) == 0) throw new IOException("App launch failed. Check the selected bundle and local session.");
        return new(false); // Activation is requested; foreground receipt is not guaranteed.
    }
    public void Power(string action)
    {
        if (action != "sleep") throw new InvalidOperationException("unsupported_action");
        if (MacNative.pc_sleep() == 0) throw new InvalidOperationException("macOS denied sleep.");
    }
}
public sealed class MacLauncherPlatform : ILauncherPlatform
{
    public StringComparer PathComparer => StringComparer.Ordinal;
    public string Normalize(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    public bool ValidExecutable(string path) => Path.IsPathFullyQualified(path) && path.EndsWith(".app", StringComparison.Ordinal) && !path.Any(char.IsControl);
    public bool Available(LauncherApp app)
    {
        if (!ValidExecutable(app.Executable) || !Directory.Exists(app.Executable)) return false;
        for (var directory = new DirectoryInfo(app.Executable); directory != null; directory = directory.Parent)
            if (directory.LinkTarget != null) return false;
        return MacNative.pc_app_valid(app.Executable) != 0;
    }
    public LauncherApp[] Discover() => []; // Local selection is explicit, never remote bundle IDs/URLs.
}
public static class MacLoginStartup
{
    public static int Status => MacNative.pc_login_status();
    public static void Set(bool enabled)
    {
        if (MacNative.pc_login_set(enabled ? 1 : 0) == 0) throw new IOException("macOS login registration failed. Use a stable installed .app and review System Settings → General → Login Items. Unsigned bundles may be ineligible.");
    }
}
