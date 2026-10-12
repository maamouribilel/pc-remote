using System.Text.Json;

namespace PcController.Core;

// Milestone 0 accepts connection diagnostics only. No input commands are exposed.
public static class DiagnosticProtocol
{
    public const int MaxBytes = 1024;
    public static bool TryPing(ReadOnlyMemory<byte> bytes, long previousSequence, out long sequence)
    {
        sequence = 0;
        if (bytes.Length == 0 || bytes.Length > MaxBytes) return false;
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            var names = root.EnumerateObject().Select(p => p.Name).ToArray();
            if (names.Length != 4 || names.Distinct().Count() != 4 ||
                names.Except(["v", "seq", "type", "payload"]).Any()) return false;
            if (!root.GetProperty("v").TryGetInt32(out var version) || version != 1 ||
                !root.GetProperty("seq").TryGetInt64(out sequence) || sequence <= previousSequence ||
                sequence > 9_007_199_254_740_991 ||
                root.GetProperty("type").ValueKind != JsonValueKind.String ||
                root.GetProperty("type").GetString() != "ping") return false;
            var payload = root.GetProperty("payload");
            return payload.ValueKind == JsonValueKind.Object && !payload.EnumerateObject().Any();
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return false;
        }
    }
}
