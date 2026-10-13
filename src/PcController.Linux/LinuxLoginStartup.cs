using System.Text;
namespace PcController.Linux;

// Local-only XDG autostart, no terminal/shell/arguments. Register the stable extracted executable.
public sealed class LinuxLoginStartup
{
    private readonly string directory;
    private readonly string executable;
    private string PathName => Path.Combine(directory, "pc-remote.desktop");
    public LinuxLoginStartup() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart"), Environment.ProcessPath ?? throw new IOException("Executable path unavailable.")) { }
    internal LinuxLoginStartup(string directory, string executable) { this.directory = directory; this.executable = executable; }
    internal static string Quote(string value)
    {
        if (!Path.IsPathFullyQualified(value) || value.Any(char.IsControl) || value.Contains('%')) throw new IOException("Unsupported startup executable path.");
        // Desktop Entry string escaping followed by Exec quoting. No shell interprets this.
        return "\"" + value.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"").Replace("`", "\\\\`").Replace("$", "\\\\$") + "\"";
    }
    private string Entry => "[Desktop Entry]\nType=Application\nName=PC Remote\nExec=" + Quote(executable) + "\nTerminal=false\nOnlyShowIn=GNOME;\nX-PCRemote-Managed=true\n";
    private void CheckPath()
    {
        for (var dir = new DirectoryInfo(directory); dir != null; dir = dir.Parent)
            if (dir.LinkTarget != null) throw new IOException("Autostart directory must not be a symlink.");
        if (OperatingSystem.IsLinux())
        {
            if (Directory.Exists(directory) && (LinuxNative.pc_linux_owned_path(directory, 1) == 0 || (File.GetUnixFileMode(directory) & (UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0))
                throw new IOException("Autostart directory must be owned by this user and not writable by other users.");
            if (File.Exists(PathName) && LinuxNative.pc_linux_owned_path(PathName, 0) == 0) throw new IOException("Autostart entry belongs to another user.");
        }
        if (new FileInfo(PathName).LinkTarget != null) throw new IOException("Autostart entry must not be a symlink.");
    }
    public bool Enabled { get { CheckPath(); return File.Exists(PathName) && File.ReadAllText(PathName) == Entry; } }
    public void Set(bool enabled)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        LinuxNative.RequireUser(); CheckPath();
        if (!File.Exists(executable) || Path.GetFileName(executable) != "PcController.Desktop" || new FileInfo(executable).LinkTarget != null)
            throw new IOException("Use the stable extracted self-contained PcController.Desktop executable for autostart.");
        if (File.Exists(PathName) && File.ReadAllText(PathName) != Entry) throw new IOException("A different startup entry exists. Review/remove that entry locally before changing this preference.");
        if (!enabled) { if (File.Exists(PathName)) File.Delete(PathName); return; }
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        CheckPath();
        var temporary = Path.Combine(directory, ".pc-remote-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var file = new FileStream(temporary, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite }))
            { file.Write(Encoding.UTF8.GetBytes(Entry)); file.Flush(true); }
            File.Move(temporary, PathName, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
