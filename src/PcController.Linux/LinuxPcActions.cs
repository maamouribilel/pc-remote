using PcController.Core;
namespace PcController.Linux;

public sealed class LinuxPcActions : IPcActions
{
    private readonly object gate = new();
    public VolumeState GetVolume() { lock (gate) return Read(-1); }
    private static VolumeState Read(int mode, int level = 0) => LinuxNative.pc_linux_volume(mode, level, out var actual, out var muted) != 0 ? new(true, actual, muted != 0) : new(false);
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
            if (!current.Available) throw new InputFailureException("Default PulseAudio/PipeWire-Pulse output unavailable.");
            if (!(action == "mute" ? Read(1) : Read(0, Math.Clamp(current.Level + (action == "volumeUp" ? 5 : -5), 0, 100))).Available)
                throw new InputFailureException("System audio change failed.");
        }
    }
    public LaunchResult Launch(LauncherApp app)
    {
        if (!new LinuxLauncherPlatform().Available(app) || LinuxNative.pc_linux_launch(app.Executable) == 0) throw new IOException("Application unavailable or launch denied.");
        return new(false); // Compositor activation may be denied; select the app locally.
    }
    public static IReadOnlySet<string> PowerCapabilities() => new[] { "sleep", "restart", "shutdown" }.Where((_, i) => LinuxNative.pc_linux_can_power(i) != 0).ToHashSet();
    public void Power(string action)
    {
        var code = action switch { "sleep" => 0, "restart" => 1, "shutdown" => 2, _ => throw new InvalidOperationException("unsupported_action") };
        if (LinuxNative.pc_linux_power(code) == 0) throw new InvalidOperationException("logind denied this action without interactive authorization.");
    }
}
public sealed class LinuxLauncherPlatform : ILauncherPlatform
{
    public StringComparer PathComparer => StringComparer.Ordinal;
    public string Normalize(string path) => Path.GetFullPath(path);
    public bool ValidExecutable(string path) => Path.IsPathFullyQualified(path) && path.EndsWith(".desktop", StringComparison.Ordinal) && !path.Any(char.IsControl);
    public bool Available(LauncherApp app)
    {
        if (!ValidExecutable(app.Executable) || !File.Exists(app.Executable) || new FileInfo(app.Executable).LinkTarget != null) return false;
        for (var directory = new DirectoryInfo(Path.GetDirectoryName(app.Executable)!); directory != null; directory = directory.Parent)
            if (directory.LinkTarget != null) return false;
        return LinuxNative.pc_linux_app_valid(app.Executable) != 0;
    }
    public LauncherApp[] Discover() => [];
}
