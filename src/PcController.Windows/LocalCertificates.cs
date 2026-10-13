using System.Net;
namespace PcController.Windows;

// Maintains the historical DPAPI files and Schannel user-key behavior.
public sealed class LocalCertificates(string directory, IReadOnlyCollection<IPAddress> addresses)
    : Host.LocalCertificates(directory, addresses, new WindowsProtectedStorage(), new WindowsCertificateKeys());
