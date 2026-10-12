using System.Text.Json;
using PcController.Core;

namespace PcController.Windows;

public sealed class LauncherSettings
{
    private readonly object gate = new();
    private readonly string path;
    private LauncherApp[] apps;
    public LauncherSettings(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "launcher-settings.json");
        apps = File.Exists(path) ? JsonSerializer.Deserialize<LauncherApp[]>(File.ReadAllBytes(path)) ?? [] : Discover();
        if (apps.Length > 24 || apps.Any(a => a.Id.Length != 32 || a.Name.Length is < 1 or > 64 || !MediaProfiles.Mappings.ContainsKey(a.Profile) || !ValidExecutable(a.Executable)) || apps.Select(a => a.Id).Distinct().Count() != apps.Length)
            throw new InvalidDataException("Invalid launcher settings.");
        if (!File.Exists(path)) Save(apps);
    }
    public LauncherApp[] List() { lock (gate) return apps.ToArray(); }
    public LauncherApp? Find(string id) { lock (gate) return apps.FirstOrDefault(a => a.Id == id); }
    public static bool ValidExecutable(string executable) => Path.IsPathFullyQualified(executable) && !executable.StartsWith(@"\\", StringComparison.Ordinal) && Path.GetExtension(executable).Equals(".exe", StringComparison.OrdinalIgnoreCase);
    public void Add(string executable, string name, string profile)
    {
        executable = Path.GetFullPath(executable); name = name.Trim();
        if (!ValidExecutable(executable) || !File.Exists(executable) || name.Length is < 1 or > 64 || name.Any(char.IsControl) || !MediaProfiles.Mappings.ContainsKey(profile)) throw new ArgumentException("Choose a local executable, a name and a media profile.");
        lock (gate)
        {
            if (apps.Length >= 24) throw new InvalidOperationException("The launcher holds up to 24 apps.");
            if (apps.Any(a => a.Executable.Equals(executable, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("This app is already in the launcher.");
            Save(apps.Append(new(Guid.NewGuid().ToString("N"), name, executable, profile)).ToArray());
        }
    }
    public void Remove(string id) { lock (gate) Save(apps.Where(a => a.Id != id).ToArray()); }
    private void Save(LauncherApp[] next)
    {
        File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(next)); File.Move(path + ".tmp", path, true); apps = next;
    }
    private static LauncherApp[] Discover()
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
