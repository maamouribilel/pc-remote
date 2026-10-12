using System.Security.Cryptography;
using System.Text.Json;
using PcController.Core;

namespace PcController.Windows;

public sealed class ProtectedDevices : IDeviceRepository
{
    private readonly object gate = new();
    private readonly string path;
    private DeviceRecord[] records;
    public ProtectedDevices(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "devices.protected");
        if (!File.Exists(path)) { records = []; return; }
        var clear = UserProtection.Unprotect(File.ReadAllBytes(path));
        try { records = JsonSerializer.Deserialize<DeviceRecord[]>(clear) ?? throw new InvalidDataException("Invalid protected device store."); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
    public DeviceRecord[] List() { lock (gate) return records.ToArray(); }
    public void Add(DeviceRecord device) { lock (gate) Save(records.Append(device).ToArray()); }
    public void Revoke(string id) { lock (gate) Save(records.Where(d => d.Id != id).ToArray()); }
    private void Save(DeviceRecord[] next)
    {
        var clear = JsonSerializer.SerializeToUtf8Bytes(next);
        try
        {
            File.WriteAllBytes(path + ".tmp", UserProtection.Protect(clear));
            File.Move(path + ".tmp", path, true);
            records = next;
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
}
