using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PcController.Host;
namespace PcController.Linux;

public sealed class LinuxStorage : IProtectedStorage
{
    private const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly string directory, account;
    public LinuxStorage(string directory)
    {
        LinuxNative.RequireUser();
        this.directory = Path.GetFullPath(directory);
        account = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(this.directory)));
        InitializeDirectory(this.directory);
    }
    public void InitializeDirectory(string path)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        path = ValidatePath(path);
        // Refuse symlinks/reparse points, including the application's existing parents.
        for (var p = path; !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
            if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("PC Remote data paths must not be symbolic links.");
        Directory.CreateDirectory(path, DirectoryMode);
        if (LinuxNative.pc_linux_owned_path(path, 1) == 0) throw new IOException("Data directory must belong to this user and be a real directory.");
        File.SetUnixFileMode(path, DirectoryMode);
        foreach (var file in Directory.EnumerateFiles(path))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("PC Remote data files must not be symbolic links.");
            if (LinuxNative.pc_linux_owned_path(file, 0) == 0) throw new IOException("Data files must be regular files belonging to this user.");
            File.SetUnixFileMode(file, FileMode);
        }
    }
    private string ValidatePath(string path)
    {
        var full = Path.GetFullPath(path);
        if (full != directory && !full.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new IOException("Protected storage path is outside the installation.");
        return full;
    }
    private byte[] Key()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        // A missing GNOME keyring item with existing ciphertext is an error, NEVER a new identity.
        bool Existing(string folder)
        {
            InitializeDirectory(folder);
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) throw new IOException("Storage subdirectories must not be symbolic links.");
                if (Existing(child)) return true;
            }
            return Directory.EnumerateFiles(folder).Any(p => p.EndsWith(".protected", StringComparison.Ordinal) || p.EndsWith(".cer", StringComparison.Ordinal));
        }
        var existing = Existing(directory);
        var status = LinuxNative.pc_linux_secret(account, key, existing ? 0 : 1);
        if (status == 0) return key;
        CryptographicOperations.ZeroMemory(key);
        throw new IOException($"GNOME login keyring unavailable (status {status}). Unlock the password-encrypted login keyring locally; restore its matching key if missing. No implicit unlock, plaintext fallback or CA reset.");
    }
    public void Probe() { var key = Key(); CryptographicOperations.ZeroMemory(key); }
    public byte[] Read(string path)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        path = ValidatePath(path); InitializeDirectory(Path.GetDirectoryName(path)!);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 32 || !bytes.AsSpan(0, 4).SequenceEqual("PCL1"u8)) throw new IOException("Invalid Linux protected envelope.");
        var key = Key(); var clear = new byte[bytes.Length - 32];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(bytes.AsSpan(4, 12), bytes.AsSpan(32), bytes.AsSpan(16, 16), clear, Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, path)));
            return clear;
        }
        catch (CryptographicException e) { CryptographicOperations.ZeroMemory(clear); throw new IOException("Protected data authentication failed; restore the matching GNOME keyring item and files.", e); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    public void Write(string path, byte[] clear)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        path = ValidatePath(path); InitializeDirectory(Path.GetDirectoryName(path)!);
        var key = Key(); var bytes = new byte[clear.Length + 32]; var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            "PCL1"u8.CopyTo(bytes); RandomNumberGenerator.Fill(bytes.AsSpan(4, 12));
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(bytes.AsSpan(4, 12), clear, bytes.AsSpan(32), bytes.AsSpan(16, 16), Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, path)));
            using (var file = new FileStream(temporary, new FileStreamOptions { Mode = System.IO.FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = FileMode }))
            { file.Write(bytes); file.Flush(true); }
            File.Move(temporary, path, true);
            File.SetUnixFileMode(path, FileMode);
        }
        finally { CryptographicOperations.ZeroMemory(key); if (File.Exists(temporary)) File.Delete(temporary); }
    }

}
public sealed class LinuxCertificateKeys : ICertificateKeyLoader
{
    public X509Certificate2 Load(byte[] pfx)
    {
        LinuxNative.RequireUser();
        return X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }
}
