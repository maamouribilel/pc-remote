using System.Text.Json;
using PcController.Host;
internal static class MouseBrowserFixture
{
    internal static async Task Run(string directory, string platform = "recording")
    {
        var mouse = new RecordingMouse();
        var pc = new RecordingPcActions();
        var capabilities = platform switch
        {
            "macos" => PcController.Core.PlatformCapabilities.ProtocolV1 with { Platform = "macos", MediaActions = new Dictionary<string, IReadOnlySet<string>> { ["General"] = new HashSet<string> { "playPause" } }, Volume = false, Launcher = false, PowerActions = new HashSet<string>() },
            "linux" => PcController.Core.PlatformCapabilities.Unavailable("linux") with { Keyboard = true, Shortcuts = true },
            "unavailable" => PcController.Core.PlatformCapabilities.Unavailable("macos"),
            _ => PcController.Core.PlatformCapabilities.ProtocolV1 with { Platform = "recording" }
        };
        await using var server = PortableFixture.Create(directory, mouse, pc, capabilities: capabilities);
        foreach (var entry in server.Launcher.List()) server.Launcher.Remove(entry.Id);
        server.Launcher.Add(PortableFixture.TestExecutable(directory),"Test player","VLC");
        server.ApproveDeviceAsync = (_, _) => Task.FromResult(true); // Test executable ONLY, loopback, recording input.
        await server.StartAsync();
        var metadata = Path.Combine(directory, "certificates", "fixture.json");
        var events = Path.Combine(directory, "certificates", "events.json");
        var pcEvents = Path.Combine(directory, "certificates", "pc-events.json");
        void Write(string path, object value) { File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(value)); File.Move(path + ".tmp", path, true); }
        Write(metadata, new { challenge = server.Pairing.CurrentChallenge!.Value });
        using var lifetime = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            while (!File.Exists(Path.Combine(directory, "stop")))
            { Write(events, mouse.Events); Write(pcEvents, pc.Events); await Task.Delay(50, lifetime.Token); }
        }
        catch (OperationCanceledException) { }
    }
}
