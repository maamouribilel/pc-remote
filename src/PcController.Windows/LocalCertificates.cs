using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Xml.Linq;

namespace PcController.Windows;

public sealed class LocalCertificates : IDisposable
{
    public X509Certificate2 Root { get; }
    public X509Certificate2 Server { get; }
    public string ProfilePath { get; }
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(Root.RawData));
    public bool Renewed { get; }

    public LocalCertificates(string directory, IReadOnlyCollection<IPAddress> addresses)
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
        var rootPath = Path.Combine(directory, "root.protected");
        Root = File.Exists(rootPath) ? Load(rootPath) : CreateRoot();
        if (Root.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(100))
            throw new InvalidOperationException("Local CA expires soon. Replace it and reinstall trust on the phone; see docs/certificates.md.");
        if (!File.Exists(rootPath)) Save(rootPath, Root);
        var names = addresses.Select(ip => ip.ToString()).Order().ToArray();
        var namesPath = Path.Combine(directory, "addresses.txt");
        var leafPath = Path.Combine(directory, "server.protected");
        X509Certificate2? leaf = File.Exists(leafPath) ? Load(leafPath) : null;
        if (leaf == null || leaf.NotAfter.ToUniversalTime() < DateTime.UtcNow.AddDays(14) ||
            !File.Exists(namesPath) || !File.ReadAllLines(namesPath).SequenceEqual(names))
        {
            leaf?.Dispose();
            leaf = CreateServer(Root, addresses);
            Save(leafPath, leaf);
            File.WriteAllLines(namesPath, names);
            Renewed = true;
        }
        Server = leaf;
        File.WriteAllBytes(Path.Combine(directory, "pc-remote-root.cer"), Root.Export(X509ContentType.Cert));
        ProfilePath = Path.Combine(directory, "pc-remote.mobileconfig");
        ExportProfile(ProfilePath);
    }

    private static X509Certificate2 CreateRoot()
    {
        using var key = RSA.Create(3072);
        var request = new CertificateRequest($"CN=PC Remote Local CA {Guid.NewGuid():N}", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(5));
        return Reload(root);
    }

    private static X509Certificate2 CreateServer(X509Certificate2 root, IEnumerable<IPAddress> addresses)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=PC Remote", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        foreach (var address in addresses.Distinct()) san.AddIpAddress(address);
        request.CertificateExtensions.Add(san.Build());
        using var certificate = request.Create(root, DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(90), RandomNumberGenerator.GetBytes(16));
        using var withKey = certificate.CopyWithPrivateKey(key);
        return Reload(withKey);
    }

    private static X509Certificate2 Reload(X509Certificate2 certificate)
    {
        var bytes = certificate.Export(X509ContentType.Pfx);
        // Schannel server credentials require a user key container on Windows.
        // Without PersistKeySet, .NET removes this temporary container at disposal.
        try { return X509CertificateLoader.LoadPkcs12(bytes, null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private static X509Certificate2 Load(string path)
    {
        var bytes = UserProtection.Unprotect(File.ReadAllBytes(path));
        try { return X509CertificateLoader.LoadPkcs12(bytes, null, X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private static void Save(string path, X509Certificate2 certificate)
    {
        var bytes = certificate.Export(X509ContentType.Pfx);
        try
        {
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, UserProtection.Protect(bytes));
            File.Move(temp, path, true);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private void ExportProfile(string path)
    {
        static object[] Pair(string key, string value) => [new XElement("key", key), new XElement("string", value)];
        var payload = new XElement("dict", Pair("PayloadType", "com.apple.security.root"),
            Pair("PayloadIdentifier", "local.pc-remote.root"), Pair("PayloadUUID", Guid.NewGuid().ToString()),
            new XElement("key", "PayloadVersion"), new XElement("integer", 1),
            Pair("PayloadDisplayName", Root.GetNameInfo(X509NameType.SimpleName, false)),
            new XElement("key", "PayloadContent"), new XElement("data", Convert.ToBase64String(Root.RawData)));
        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
            new XElement("plist", new XAttribute("version", "1.0"), new XElement("dict",
                Pair("PayloadType", "Configuration"), Pair("PayloadIdentifier", "local.pc-remote"),
                Pair("PayloadUUID", Guid.NewGuid().ToString()), new XElement("key", "PayloadVersion"), new XElement("integer", 1),
                Pair("PayloadDisplayName", "PC Remote — local certificate"),
                Pair("PayloadDescription", "Trust only for your own PC. Contains a public certificate, no private key."),
                new XElement("key", "PayloadContent"), new XElement("array", payload))));
        document.Save(path);
    }
    public void Dispose() { Server.Dispose(); Root.Dispose(); }
}
