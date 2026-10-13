using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
namespace PcController.Host;
public static class CertificateIssuer
{
    public static X509Certificate2 CreateRoot()
    {
        using var key = RSA.Create(3072);
        var request = new CertificateRequest($"CN=PC Remote Local CA {Guid.NewGuid():N}", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        var root = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(5));
        return root;
    }

    public static X509Certificate2 CreateServer(X509Certificate2 root, IEnumerable<IPAddress> addresses)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=PC Remote", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(root, true, false));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        foreach (var address in addresses.Distinct()) san.AddIpAddress(address);
        request.CertificateExtensions.Add(san.Build());
        using var certificate = request.Create(root, DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(90), RandomNumberGenerator.GetBytes(16));
        return certificate.CopyWithPrivateKey(key);
    }

}
