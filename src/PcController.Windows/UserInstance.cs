using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace PcController.Windows;

public enum LocalAction : byte { Open = 1, Pause = 2, Resume = 3, Exit = 4 }

// Same-user local IPC only; no network endpoint or arbitrary command strings.
public sealed class UserInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly CancellationTokenSource stop = new();
    private readonly string pipeName;
    private bool disposed;
    public bool IsPrimary { get; }
    public UserInstance(string suffix = "")
    {
        var identity = WindowsIdentity.GetCurrent().User!.Value;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity + suffix)))[..24];
        pipeName = "PcRemote-" + key;
        mutex = new Mutex(true, "Local\\" + pipeName, out var created);
        IsPrimary = created;
    }
    public async Task<bool> SendAsync(LocalAction action, bool waitForExit = false)
    {
        if (!Enum.IsDefined(action)) return false;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(timeout.Token);
            await pipe.WriteAsync(new byte[] { (byte)action }, timeout.Token);
            var reply = new byte[action == LocalAction.Exit ? 5 : 1];
            await pipe.ReadExactlyAsync(reply, timeout.Token);
            if (reply[0] != 1) return false;
            if (action == LocalAction.Exit && waitForExit)
            {
                var processId = BitConverter.ToInt32(reply, 1);
                if (processId <= 0 || processId == Environment.ProcessId) return false;
                try
                {
                    using var primary = System.Diagnostics.Process.GetProcessById(processId);
                    await primary.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
                }
                catch (ArgumentException) { /* Primary already exited. */ }
                catch (TimeoutException) { return false; }
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException or UnauthorizedAccessException) { return false; }
    }
    public async Task ListenAsync(Func<LocalAction, Task> handle)
    {
        if (!IsPrimary) throw new InvalidOperationException("Only the primary instance can listen.");
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(stop.Token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token); timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var command = new byte[1];
                if (await pipe.ReadAsync(command, timeout.Token) != 1 || !Enum.IsDefined((LocalAction)command[0])) continue;
                var action = (LocalAction)command[0];
                // Acknowledge Exit before shutdown cancels this listener's token.
                if (action == LocalAction.Exit)
                {
                    var reply = new byte[5]; reply[0] = 1;
                    BitConverter.GetBytes(Environment.ProcessId).CopyTo(reply, 1);
                    await pipe.WriteAsync(reply, timeout.Token);
                }
                await handle(action);
                if (action != LocalAction.Exit) await pipe.WriteAsync(new byte[] { 1 }, timeout.Token);
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or UnauthorizedAccessException) { }
        }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        stop.Cancel();
        // Mutex ownership is released on the creating UI thread.
        if (IsPrimary) mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
