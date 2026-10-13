using PcController.Core;
namespace PcController.Windows;
public sealed class WindowsLauncherPlatform : ILauncherPlatform
{
    public string Normalize(string path) => Path.GetFullPath(path);
    public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
    public bool Available(LauncherApp app) => File.Exists(app.Executable);
    public bool ValidExecutable(string executable) => Path.IsPathFullyQualified(executable) && !executable.StartsWith(@"\\", StringComparison.Ordinal) && Path.GetExtension(executable).Equals(".exe", StringComparison.OrdinalIgnoreCase);
    public LauncherApp[] Discover()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var program = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var program32 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var found = new List<LauncherApp>();
        void Add(string name, string profile, params string[] candidates)
        {
            var executable = candidates.FirstOrDefault(File.Exists);
            if (executable != null) found.Add(new(Guid.NewGuid().ToString("N"), name, Path.GetFullPath(executable), profile));
        }
        var stremioRoot = Path.Combine(local, "Programs", "LNV", "Stremio-4");
        Add("Stremio", "Stremio", Path.Combine(local,"Programs","Stremio","stremio-shell-ng.exe"), Path.Combine(local,"Programs","Stremio","stremio.exe"), Path.Combine(local,"Programs","LNV","Stremio","stremio.exe"), Path.Combine(stremioRoot,"stremio.exe"), Path.Combine(roaming,"stremio","stremio.exe"), Path.Combine(local,"stremio","stremio.exe"), Path.Combine(program,"Stremio","stremio.exe"));
        Add("VLC", "VLC", Path.Combine(program,"VideoLAN","VLC","vlc.exe"), Path.Combine(program32,"VideoLAN","VLC","vlc.exe"));
        Add("Chrome", "YouTube", Path.Combine(program,"Google","Chrome","Application","chrome.exe"), Path.Combine(program32,"Google","Chrome","Application","chrome.exe"), Path.Combine(local,"Google","Chrome","Application","chrome.exe"));
        Add("Edge", "YouTube", Path.Combine(program32,"Microsoft","Edge","Application","msedge.exe"), Path.Combine(program,"Microsoft","Edge","Application","msedge.exe"));
        return found.ToArray();
    }
}
