using System.Text.Json;
using PcController.Core;

namespace PcController.Host;

public class LauncherSettings : ILauncherSettings
{
    private readonly object gate = new();
    private readonly string path;
    private LauncherApp[] apps;
    private readonly ILauncherPlatform platform;
    public LauncherSettings(string directory, ILauncherPlatform platform)
    {
        this.platform = platform;
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "launcher-settings.json");
        apps = File.Exists(path) ? JsonSerializer.Deserialize<LauncherApp[]>(File.ReadAllBytes(path)) ?? [] : platform.Discover();
        if (apps.Length > 24 || apps.Any(a => a.Id.Length != 32 || a.Name.Length is < 1 or > 64 || !MediaProfiles.Actions.ContainsKey(a.Profile) || !platform.ValidExecutable(a.Executable)) || apps.Select(a => a.Id).Distinct().Count() != apps.Length)
            throw new InvalidDataException("Invalid launcher settings.");
        if (!File.Exists(path)) Save(apps);
    }
    public bool Available(LauncherApp app) => platform.Available(app);
    public LauncherApp[] List() { lock (gate) return apps.ToArray(); }
    public LauncherApp? Find(string id) { lock (gate) return apps.FirstOrDefault(a => a.Id == id); }
    public void Add(string executable, string name, string profile)
    {
        executable = platform.Normalize(executable); name = name.Trim();
        if (!platform.ValidExecutable(executable) || !platform.Available(new("", name, executable, profile)) || name.Length is < 1 or > 64 || name.Any(char.IsControl) || !MediaProfiles.Actions.ContainsKey(profile)) throw new ArgumentException("Choose a local executable, a name and a media profile.");
        lock (gate)
        {
            if (apps.Length >= 24) throw new InvalidOperationException("The launcher holds up to 24 apps.");
            if (apps.Any(a => platform.PathComparer.Equals(a.Executable, executable))) throw new InvalidOperationException("This app is already in the launcher.");
            Save(apps.Append(new(Guid.NewGuid().ToString("N"), name, executable, profile)).ToArray());
        }
    }
    public void Remove(string id) { lock (gate) Save(apps.Where(a => a.Id != id).ToArray()); }
    private void Save(LauncherApp[] next)
    {
        File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(next)); File.Move(path + ".tmp", path, true); apps = next;
    }
}
