using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PcController.Host;
namespace PcController.MacOS;

public sealed class MacStorage : IProtectedStorage
{
    private const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly string directory, account;
    private readonly string? testKeychain;
    public MacStorage(string directory)
    {
        MacNative.RequireUser();
        this.directory = Path.GetFullPath(directory);
        account = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(this.directory)));
        InitializeDirectory(this.directory);
    }
    internal MacStorage(string directory, string testAccount, string? testKeychain = null) : this(directory)
    { if (!testAccount.StartsWith("test-", StringComparison.Ordinal)) throw new ArgumentException("Test namespace required."); account = testAccount; this.testKeychain = testKeychain; }
    public void InitializeDirectory(string path)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        path = ValidatePath(path);
        // Refuse symlinks/reparse points, including the application's existing parents.
        for (var p = path; !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
            if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("PC Remote data paths must not be symbolic links.");
        Directory.CreateDirectory(path, DirectoryMode);
        File.SetUnixFileMode(path, DirectoryMode);
        foreach (var file in Directory.EnumerateFiles(path))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("PC Remote data files must not be symbolic links.");
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
        // A missing Keychain item with existing ciphertext is an error, NEVER a new identity.
        var existing = Directory.EnumerateFiles(directory, "*.protected", SearchOption.AllDirectories).Any();
        var status = testKeychain == null ? MacNative.pc_secret(account, key, existing ? 0 : 1) : MacNative.pc_test_secret(testKeychain, account, key, existing ? 0 : 1);
        if (status == 0) return key;
        CryptographicOperations.ZeroMemory(key);
        throw new IOException($"Keychain unavailable (OSStatus {status}). Unlock the per-user default Keychain and retry the same app; after an update/rebuild, choose Allow Keychain access… and approve this PC Remote.app in the macOS dialog; restore its storage key if missing. No plaintext fallback or CA reset is performed.");
    }
    public void Probe() { var key = Key(); CryptographicOperations.ZeroMemory(key); }
    // Explicit local recovery after an update/rebuild changes the unsigned app's code identity.
    // macOS asks for the user's keychain password; an existing key is authorized, never created/reset.
    public void AuthorizeExistingKey()
    {
        if (testKeychain != null) throw new InvalidOperationException("Disposable test storage never requests Keychain authorization.");
        if (!Directory.EnumerateFiles(directory, "*.protected", SearchOption.AllDirectories).Any())
            throw new IOException("No existing protected data needs Keychain authorization. A new installation creates its own storage key.");
        var status = MacNative.pc_secret_authorize(account);
        if (status != 0) throw new IOException($"Keychain access was not granted (OSStatus {status}). Unlock your login Keychain if needed, retry and choose Always Allow for this PC Remote.app. No key, certificate or remembered device was reset.");
    }
    public byte[] Read(string path)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        path = ValidatePath(path); InitializeDirectory(Path.GetDirectoryName(path)!);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 32 || !bytes.AsSpan(0, 4).SequenceEqual("PCM1"u8)) throw new IOException("Invalid macOS protected envelope.");
        var key = Key(); var clear = new byte[bytes.Length - 32];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(bytes.AsSpan(4, 12), bytes.AsSpan(32), bytes.AsSpan(16, 16), clear, Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, path)));
            return clear;
        }
        catch (CryptographicException e) { CryptographicOperations.ZeroMemory(clear); throw new IOException("Protected data authentication failed; restore the matching Keychain item and files.", e); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    public void Write(string path, byte[] clear)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        path = ValidatePath(path); InitializeDirectory(Path.GetDirectoryName(path)!);
        var key = Key(); var bytes = new byte[clear.Length + 32]; var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            "PCM1"u8.CopyTo(bytes); RandomNumberGenerator.Fill(bytes.AsSpan(4, 12));
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(bytes.AsSpan(4, 12), clear, bytes.AsSpan(32), bytes.AsSpan(16, 16), Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, path)));
            using (var file = new FileStream(temporary, new FileStreamOptions { Mode = System.IO.FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = FileMode }))
            { file.Write(bytes); file.Flush(true); }
            File.Move(temporary, path, true);
            File.SetUnixFileMode(path, FileMode);
        }
        finally { CryptographicOperations.ZeroMemory(key); if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal void DeleteTestSecret() { var status = MacNative.pc_delete_test_secret(account); if (status != 0) throw new IOException($"Test Keychain cleanup failed: {status}"); }
}
public sealed class MacCertificateKeys : ICertificateKeyLoader
{
    public X509Certificate2 Load(byte[] pfx)
    {
        MacNative.RequireUser();
        // macOS .NET does not support EphemeralKeySet. Default imports are temporary;
        // persistent PFX protection is the Keychain-backed envelope, not key import.
        return X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.Exportable);
    }
}
