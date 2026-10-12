using System.Text.Json;
using PcController.Core;
namespace PcController.Windows;
public sealed class MediaSettings
{
    private readonly object gate = new();
    private readonly string path;
    private string profile;
    public string Profile { get { lock (gate) return profile; } }
    public MediaSettings(string directory)
    {
        path = Path.Combine(directory, "media-settings.json");
        profile = File.Exists(path) ? JsonSerializer.Deserialize<string>(File.ReadAllBytes(path)) ?? "General" : "General";
        if (!MediaProfiles.Mappings.ContainsKey(profile)) throw new InvalidDataException("Invalid media profile.");
    }
    public void Save(string next)
    {
        if (!MediaProfiles.Mappings.ContainsKey(next)) throw new ArgumentOutOfRangeException(nameof(next));
        lock (gate) { File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(next)); File.Move(path + ".tmp", path, true); profile = next; }
    }
}
