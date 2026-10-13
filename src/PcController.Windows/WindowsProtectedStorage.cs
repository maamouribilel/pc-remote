using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using PcController.Host;
namespace PcController.Windows;

public sealed class WindowsProtectedStorage : IProtectedStorage
{
    public void InitializeDirectory(string directory)
    {
        var info = Directory.CreateDirectory(directory);
        var acl = new DirectorySecurity();
        acl.SetAccessRuleProtection(true, false);
        var inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,
            FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        info.SetAccessControl(acl);
    }
    public byte[] Read(string path) => UserProtection.Unprotect(File.ReadAllBytes(path));
    public void Write(string path, byte[] clear)
    {
        File.WriteAllBytes(path + ".tmp", UserProtection.Protect(clear));
        File.Move(path + ".tmp", path, true);
    }
}
public sealed class WindowsCertificateKeys : ICertificateKeyLoader
{
    // Schannel needs a user key container. No PersistKeySet: disposal removes it.
    public X509Certificate2 Load(byte[] pfx) => X509CertificateLoader.LoadPkcs12(pfx, null,
        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
}
