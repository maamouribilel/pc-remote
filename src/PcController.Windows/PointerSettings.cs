using System.Text.Json;
using PcController.Core;
namespace PcController.Windows;
public sealed class PointerSettings
{
    private readonly string path;
    public PointerPreferences Value { get; private set; }
    public PointerSettings(string directory)
    {
        path = Path.Combine(directory, "pointer-settings.json");
        Value = File.Exists(path) ? JsonSerializer.Deserialize<PointerPreferences>(File.ReadAllBytes(path)) ?? new() : new();
        if (!Value.Valid) throw new InvalidDataException("Invalid pointer settings.");
    }
    public void Save(PointerPreferences value)
    {
        if (!value.Valid) throw new ArgumentOutOfRangeException(nameof(value));
        File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(value)); File.Move(path + ".tmp", path, true); Value = value;
    }
}
