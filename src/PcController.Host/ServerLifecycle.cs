using PcController.Core;
using System.IO;

namespace PcController.Host;

public sealed class ServerLifecycle(Func<NetworkSnapshot, CompanionServer> create, Func<NetworkSnapshot> read) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed, paused;
    public CompanionServer? Server { get; private set; }
    public event Action? Updating;
    public event Action<CompanionServer>? Ready;
    public event Action<string>? Failed;
    private static string Signature(NetworkSnapshot snapshot) => string.Join(",", snapshot.PrivateAddresses.Select(a => a.ToString()).Order()) + "|" + string.Join(",", snapshot.AllAddresses.Select(a => a.ToString()).Order()) + "|" + snapshot.Notice + "|" + snapshot.TrustIdentity;
    public async Task RefreshAsync(bool force = false)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed) return;
            var network = read();
            if (!force && Server is { } current && Signature(network) == Signature(current.Network) && current.Certificates.Server.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddDays(14)) return;
            Updating?.Invoke();
            if (Server is { } previous)
            {
                paused = previous.Sessions.Paused;
                previous.Sessions.Shutdown();
                try { previous.Input.ReleaseAll(); } catch (InputFailureException) { paused = true; }
                Server = null;
                await previous.DisposeAsync().ConfigureAwait(false); // Release and stop old listeners before binding replacements.
            }
            CompanionServer? next = null;
            try
            {
                next = await Task.Run(() => create(network)).ConfigureAwait(false);
                if (Signature(next.Network) != Signature(network)) throw new InvalidOperationException("Factory changed approved listener snapshot.");
                if (Signature(read()) != Signature(network)) throw new InvalidOperationException("Network changed during host setup.");
                next.Sessions.Pause(paused || next.Sessions.Paused);
                await next.StartAsync().ConfigureAwait(false);
                if (Signature(read()) != Signature(network)) throw new InvalidOperationException("Network changed while starting listeners.");
                Server = next; Ready?.Invoke(next);
            }
            catch (Exception exception)
            {
                if (next != null) await next.DisposeAsync().ConfigureAwait(false);
                Failed?.Invoke(exception is IOException or UnauthorizedAccessException or InvalidOperationException
                    ? "HTTPS setup failed: " + exception.Message + " Control remains paused; resolve setup and retry."
                    : "HTTPS setup failed. Check port 8443, the trusted network selection, certificate expiry and the user data folder. Retrying automatically.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Net.NetworkInformation.NetworkInformationException)
        {
            paused = true;
            if (Server is { } unsafeServer)
            {
                Server = null;
                await unsafeServer.DisposeAsync().ConfigureAwait(false);
            }
            Failed?.Invoke("Network inspection or input cleanup failed. Control is paused; check the interactive desktop and network settings. Retrying automatically.");
        }
        finally { gate.Release(); }
    }
    public async Task StopAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            paused = true; Updating?.Invoke();
            if (Server is { } server) { Server = null; await server.DisposeAsync().ConfigureAwait(false); }
        }
        finally { gate.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { disposed = true; if (Server != null) { await Server.DisposeAsync().ConfigureAwait(false); Server = null; } }
        finally { gate.Release(); }
    }
}
