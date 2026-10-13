using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using PcController.Core;

namespace PcController.Desktop;

public sealed record NetworkCandidate(string Id, string Name, string Identity, IPAddress[] Addresses)
{
    public override string ToString() => $"{Name} · {string.Join(", ", Addresses.Select(a => a.ToString()))}";
}

// Approval is deliberately session-only. Connectivity events invalidate even an unchanged
// address/gateway signature (e.g. roaming between Wi-Fi networks with the same subnet).
public sealed class TrustedNetworks(Func<NetworkCandidate[]>? inspect = null) : ITrustedNetworkDiscovery
{
    private readonly object gate = new();
    private string? approved;
    private long generation;
    private NetworkCandidate[] candidates = [];
    public NetworkCandidate[] Candidates { get { lock (gate) return candidates.ToArray(); } }
    public void Invalidate() { lock (gate) { approved = null; generation++; } }
    public bool Approve(NetworkCandidate selection)
    {
        lock (gate)
        {
            Read();
            var current = candidates.FirstOrDefault(c => c.Id == selection.Id && c.Identity == selection.Identity);
            if (current == null) return false;
            approved = current.Identity; return true;
        }
    }
    public NetworkSnapshot Read()
    {
        lock (gate)
        {
            try { candidates = (inspect ?? Inspect)(); }
            catch (Exception e) when (e is NetworkInformationException or SocketException or UnauthorizedAccessException)
            { candidates = []; approved = null; generation++; }
            var selected = candidates.FirstOrDefault(c => c.Identity == approved);
            if (approved != null && selected == null) { approved = null; generation++; }
            return new(selected?.Addresses ?? [], candidates.SelectMany(c => c.Addresses).Distinct().ToArray(),
                selected == null ? "LAN blocked. Select and explicitly approve a trusted interface for this session." : "Network approved for this session. Connectivity changes require approval again.",
                $"{generation}:{approved ?? "unapproved"}");
        }
    }
    private static string? ReadNetworkIdentity(NetworkInterface network)
    {
        if (network.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) return "wired-session";
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsMacOS() ? "/usr/sbin/networksetup" : "/usr/bin/nmcli",
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            var arguments = OperatingSystem.IsMacOS()
                ? new[] { "-getairportnetwork", network.Name }
                : new[] { "-t", "-f", "ACTIVE,SSID,BSSID", "device", "wifi", "list", "ifname", network.Name, "--rescan", "no" };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(1000)) { process.Kill(true); return null; }
            var value = output.GetAwaiter().GetResult(); _ = errors.GetAwaiter().GetResult();
            if (process.ExitCode != 0 || value.Length > 4096) return null;
            if (OperatingSystem.IsMacOS())
            {
                const string prefix = "Current Wi-Fi Network: ";
                if (!value.StartsWith(prefix, StringComparison.Ordinal)) return null;
                var name = value[prefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(name) || name.Contains("redacted", StringComparison.OrdinalIgnoreCase) ? null : name;
            }
            return value.Split('\n').FirstOrDefault(line => line.StartsWith("yes:", StringComparison.Ordinal));
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException) { return null; }
    }
    private static NetworkCandidate[] Inspect() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
        .Select(n =>
        {
            var properties = n.GetIPProperties();
            // No RFC1918 trust inference; all suitable interfaces are candidates only.
            var addresses = properties.UnicastAddresses.Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a) && a.GetAddressBytes()[0] != 169)
                .OrderBy(a => a.ToString()).ToArray();
            var gateways = properties.GatewayAddresses.Select(g => g.Address.ToString()).Order();
            var networkName = ReadNetworkIdentity(n);
            // Wi-Fi without an observable network name cannot receive approval. Do not
            // request Location permission or invent an identity from its private address.
            if (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && networkName == null)
                return new NetworkCandidate(n.Id, n.Name, "", []);
            var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                n.Id + "|" + networkName + "|" + n.GetPhysicalAddress() + "|" + string.Join(",", gateways) + "|" + string.Join(",", addresses.Select(a => a.ToString())))));
            return new NetworkCandidate(n.Id, n.Name, identity, addresses);
        }).Where(c => c.Addresses.Length > 0).ToArray();
}
