using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace PcController.Host;

public class LocalCertificates : ILocalCertificates
{
    public X509Certificate2 Root { get; }
    public X509Certificate2 Server { get; }
    public string ProfilePath { get; }
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(Root.RawData));
    public bool Renewed { get; }

    private readonly IProtectedStorage storage;
    private readonly ICertificateKeyLoader keys;
    public LocalCertificates(string directory, IReadOnlyCollection<IPAddress> addresses, IProtectedStorage storage, ICertificateKeyLoader keys)
    {
        this.storage = storage; this.keys = keys;
        storage.InitializeDirectory(directory);
        var rootPath = Path.Combine(directory, "root.protected");
        Root = File.Exists(rootPath) ? Load(rootPath) : Generate(() => CertificateIssuer.CreateRoot());
        X509Certificate2? leaf = null;
        try
        {
            if (Root.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(100))
                throw new InvalidOperationException("Local CA expires soon. Replace it and reinstall trust on the phone; see docs/certificates.md.");
            if (!File.Exists(rootPath)) Save(rootPath, Root);
            var names = addresses.Select(ip => ip.ToString()).Order().ToArray();
            var namesPath = Path.Combine(directory, "addresses.txt");
            var leafPath = Path.Combine(directory, "server.protected");
            leaf = File.Exists(leafPath) ? Load(leafPath) : null;
            if (leaf == null || leaf.NotAfter.ToUniversalTime() < DateTime.UtcNow.AddDays(14) ||
                leaf.Extensions["2.5.29.35"] == null || leaf.Extensions["2.5.29.14"] == null ||
                !File.Exists(namesPath) || !File.ReadAllLines(namesPath).SequenceEqual(names))
            {
                leaf?.Dispose();
                leaf = Generate(() => CertificateIssuer.CreateServer(Root, addresses));
                Save(leafPath, leaf);
                File.WriteAllLines(namesPath, names);
                Renewed = true;
            }
            Server = leaf;
            File.WriteAllBytes(Path.Combine(directory, "pc-remote-root.cer"), Root.Export(X509ContentType.Cert));
            ProfilePath = Path.Combine(directory, "pc-remote.mobileconfig");
            ExportProfile(ProfilePath);
        }
        catch { leaf?.Dispose(); Root.Dispose(); throw; }
    }

    private X509Certificate2 Generate(Func<X509Certificate2> create)
    {
        using var certificate = create();
        var bytes = certificate.Export(X509ContentType.Pfx);
        try { return keys.Load(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private X509Certificate2 Load(string path)
    {
        var bytes = storage.Read(path);
        try { return keys.Load(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private void Save(string path, X509Certificate2 certificate)
    {
        var bytes = certificate.Export(X509ContentType.Pfx);
        try { storage.Write(path, bytes); }
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
