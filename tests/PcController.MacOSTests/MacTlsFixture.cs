using PcController.Core;
namespace PcController.MacOS;
internal static class MacTlsFixture
{
    public static async Task Run(string directory)
    {
        var storage = new MacStorage(directory, "test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new MacHostFactory(directory, storage, new MacInput());
            await using var server = factory.Create(new([], [], "MAC KEYCHAIN TLS FIXTURE · loopback only"));
            // Always paused, no approval callback. This fixture cannot grant input ownership.
            server.Sessions.Pause(true); await server.StartAsync();
            File.WriteAllText(Path.Combine(directory, "certificates", "fixture.json"), "{}");
            while (!File.Exists(Path.Combine(directory, "stop"))) await Task.Delay(100);
        }
        finally { try { storage.DeleteTestSecret(); } catch (IOException) { } }
    }
}
