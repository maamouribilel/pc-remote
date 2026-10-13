using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace PcController.Linux;
public sealed class LinuxSingleInstance : IDisposable
{
    private readonly FileStream file;
    [DllImport("libc", SetLastError = true)] private static extern int flock(int fd, int operation);
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags, int mode);
    private LinuxSingleInstance(FileStream file) => this.file = file;
    public static LinuxSingleInstance? TryAcquire()
    {
        LinuxNative.RequireUser(); LinuxNative.umask(0x3f);
        var storage = new LinuxStorage(LinuxHostFactory.DefaultDirectory);
        storage.InitializeDirectory(LinuxHostFactory.DefaultDirectory);
        var fd = open(Path.Combine(LinuxHostFactory.DefaultDirectory, "instance.lock"), 2 | 64 | 131072 | 524288, 384); // RDWR CREAT NOFOLLOW CLOEXEC 0600
        if (fd < 0) throw new IOException("Per-user Linux instance lock could not be opened.");
        var file = new FileStream(new SafeFileHandle((IntPtr)fd, true), FileAccess.ReadWrite);
        if (flock(fd, 2 | 4) == 0) return new(file);
        var error = Marshal.GetLastPInvokeError(); file.Dispose();
        if (error != 11) throw new IOException("Linux instance lock could not be acquired.");
        return null;
    }
    public void Dispose() { flock(file.SafeFileHandle.DangerousGetHandle().ToInt32(), 8); file.Dispose(); }
}
