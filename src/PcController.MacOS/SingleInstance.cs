using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace PcController.MacOS;

public sealed class SingleInstance : IDisposable
{
    private readonly FileStream file;
    [DllImport("libc", SetLastError = true)] private static extern int flock(int descriptor, int operation);
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags, int mode);
    private SingleInstance(FileStream file) => this.file = file;
    public static SingleInstance? TryAcquire()
    {
        MacNative.RequireUser(); MacNative.umask(0x3f);
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "PC Remote");
        var storage = new MacStorage(directory); storage.InitializeDirectory(directory);
        var path = Path.Combine(directory, "instance.lock");
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Instance lock must not be a symbolic link.");
        // FileStream(path) takes its own shared flock on Unix, which fails before
        // we can report a second instance. Open directly, then own the only flock.
        var descriptor = open(path, 2 | 0x200 | 0x100 | 0x1000000, 0x180); // RDWR, CREAT, NOFOLLOW, CLOEXEC; 0600
        if (descriptor < 0) throw new IOException("The per-user instance lock could not be opened.");
        var file = new FileStream(new SafeFileHandle((IntPtr)descriptor, ownsHandle: true), FileAccess.ReadWrite);
        if (flock(file.SafeFileHandle.DangerousGetHandle().ToInt32(), 2 | 4) == 0) return new(file);
        var error = Marshal.GetLastPInvokeError(); file.Dispose();
        if (error != 35) throw new IOException("The per-user instance lock could not be acquired.");
        return null;
    }
    public void Dispose() { flock(file.SafeFileHandle.DangerousGetHandle().ToInt32(), 8); file.Dispose(); }
}
