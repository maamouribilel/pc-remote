using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using PcController.Core;
using PcController.Host;

if (args.Length is 2 or 3 && args[0] == "--serve-mouse-fixture") { await MouseBrowserFixture.Run(args[1], args.Length == 3 ? args[2] : "recording"); return; }
var passed = 0;
void Check(bool success, string name)
{
    if (!success) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
MouseChecks.Run(Check); KeyboardChecks.Run(Check); MediaChecks.Run(Check); PcActionChecks.Run(Check);
await PairingChecks.Run(Check, true);
await LifecycleChecks.Network(Check);
await TransportChecks.Run(Check);
await PcEndpointChecks.Run(Check);
await SecurityChecks.Run(Check);
byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
var valid = "{\"v\":1,\"seq\":1,\"type\":\"ping\",\"payload\":{}}";
Check(DiagnosticProtocol.TryPing(Bytes(valid), 0, out var seq) && seq == 1, "versioned ping");
foreach (var invalid in new[] { "{", "null", "[]", valid.Replace("ping", "key.down"), valid.Replace("\"v\":1", "\"v\":2"),
    valid.Replace("\"seq\":1", "\"seq\":-1"), valid.Replace("\"seq\":1", "\"seq\":1.5"),
    valid.Replace("\"seq\":1", "\"seq\":9007199254740992"), valid.Replace("\"payload\":{}", "\"payload\":{\"text\":\"x\"}"),
    valid.Replace("\"v\":1", "\"v\":1,\"v\":1"), valid.Replace("\"v\":1", "\"v\":null") })
    Check(!DiagnosticProtocol.TryPing(Bytes(invalid), 0, out _), "reject malformed/unsupported ping");
Check(!DiagnosticProtocol.TryPing(Bytes(valid), 1, out _), "reject sequence replay");
Check(!DiagnosticProtocol.TryPing(new byte[1025], 0, out _), "message bound");
var testDirectory = Path.Combine(Path.GetTempPath(), "PcControllerTests", Guid.NewGuid().ToString("N"));
using (var first = new LocalCertificates(testDirectory, [IPAddress.Loopback], new TestProtectedStorage(), new TestCertificateKeys()))
{
    Check(first.Renewed && first.Root.HasPrivateKey && first.Server.HasPrivateKey, "initial certificate generation");
    Check(first.Server.Extensions["2.5.29.35"]?.RawData.SequenceEqual(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(first.Root, true, false).RawData) == true, "server authority key identifier matches the trusted root");
    Check(first.Server.Extensions["2.5.29.14"] != null, "server certificate has a subject key identifier for strict validators");
    using var next = new LocalCertificates(testDirectory, [IPAddress.Loopback], new TestProtectedStorage(), new TestCertificateKeys());
    Check(!next.Renewed && next.Fingerprint == first.Fingerprint && next.Server.Thumbprint == first.Server.Thumbprint, "protected persistence");
    // Model an existing 0.2.0 installation whose valid leaf omitted AKI/SKI.
    // Matching addresses/expiry must not suppress its safe leaf-only migration.
    using (var legacyKey = RSA.Create(2048))
    {
        var legacyRequest = new CertificateRequest("CN=PC Remote", legacyKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        legacyRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        legacyRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        legacyRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        var legacySan = new SubjectAlternativeNameBuilder(); legacySan.AddDnsName("localhost"); legacySan.AddIpAddress(IPAddress.Loopback);
        legacyRequest.CertificateExtensions.Add(legacySan.Build());
        using var legacyPublic = legacyRequest.Create(first.Root, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(90), RandomNumberGenerator.GetBytes(16));
        using var legacy = legacyPublic.CopyWithPrivateKey(legacyKey); var bytes = legacy.Export(X509ContentType.Pfx);
        try { File.WriteAllBytes(Path.Combine(testDirectory, "server.protected"), bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        using var migrated = new LocalCertificates(testDirectory, [IPAddress.Loopback], new TestProtectedStorage(), new TestCertificateKeys());
        Check(migrated.Renewed && migrated.Fingerprint == first.Fingerprint && migrated.Server.Thumbprint != legacy.Thumbprint && migrated.Server.Extensions["2.5.29.35"] != null,
            "legacy server certificate renews without replacing the trusted root");
    }
    using var changed = new LocalCertificates(testDirectory, [IPAddress.Loopback, IPAddress.Parse("192.0.2.10")], new TestProtectedStorage(), new TestCertificateKeys());
    Check(changed.Renewed && changed.Fingerprint == first.Fingerprint && changed.Server.Thumbprint != first.Server.Thumbprint, "SAN regeneration preserves root");
    var profile = XDocument.Load(first.ProfilePath);
    using var publicRoot = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(profile.Descendants("data").Single().Value));
    Check(!publicRoot.HasPrivateKey && publicRoot.Thumbprint == first.Root.Thumbprint, "export contains public root only");
    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(first.Root);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
    Check(chain.Build(first.Server), "leaf chain and server EKU validate against installation root");
}
// Clean up only the unique, explicitly created test directory.
Directory.Delete(testDirectory, true);

await CapabilityChecks.Run(Check);
Console.WriteLine($"{passed} portable checks passed.");
