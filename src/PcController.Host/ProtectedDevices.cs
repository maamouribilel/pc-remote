using System.Security.Cryptography;
using System.Text.Json;
using PcController.Core;

namespace PcController.Host;

public class ProtectedDevices : IDeviceRepository
{
    private readonly object gate = new();
    private readonly string path;
    private DeviceRecord[] records;
    private readonly IProtectedStorage storage;
    public ProtectedDevices(string directory, IProtectedStorage storage)
    {
        this.storage = storage;
        storage.InitializeDirectory(directory);
        path = Path.Combine(directory, "devices.protected");
        if (!File.Exists(path)) { records = []; return; }
        var clear = storage.Read(path);
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
            storage.Write(path, clear);
            records = next;
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
}
