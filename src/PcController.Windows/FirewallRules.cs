using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PcController.Windows;

public static class FirewallRules
{
    private const string Group = "PC Remote installed application";
    public static string RuleName(string executable) => "PcRemote-Private-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())))[..16];
    private static bool Owned(dynamic rule, string executable) =>
        string.Equals((string)rule.ApplicationName, Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase) &&
        (string)rule.Grouping == Group && (int)rule.Protocol == 6 && (string)rule.LocalPorts == "8443" && (int)rule.Profiles == 2;
    public static bool Exists(string executable)
    {
        object? policy = null, rules = null, existing = null;
        try
        {
            policy = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2", true)!);
            rules = ((dynamic)policy!).Rules;
            try { existing = ((dynamic)rules!).Item(RuleName(executable)); }
            catch (Exception e) when (e is COMException or FileNotFoundException && e.HResult == unchecked((int)0x80070002)) { return false; }
            return Owned(existing!, executable);
        }
        finally { foreach (var item in new[] { existing, rules, policy }) if (item != null && Marshal.IsComObject(item)) Marshal.FinalReleaseComObject(item); }
    }
    public static void Configure(string executable, bool install)
    {
        object? policy = null, rules = null, existing = null, created = null;
        try
        {
            policy = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2", true)!); rules = ((dynamic)policy!).Rules;
            try { existing = ((dynamic)rules!).Item(RuleName(executable)); }
            catch (Exception e) when (e is COMException or FileNotFoundException && e.HResult == unchecked((int)0x80070002)) { }
            if (existing != null)
            {
                if (!Owned(existing, executable)) throw new InvalidOperationException("An unrelated firewall rule has the same name.");
                ((dynamic)rules!).Remove(RuleName(executable));
            }
            if (!install) return;
            created = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule", true)!); dynamic rule = created!;
            rule.Name = RuleName(executable); rule.Description = "PC Remote HTTPS/WSS, Private LocalSubnet only";
            rule.ApplicationName = Path.GetFullPath(executable); rule.Grouping = Group; rule.Protocol = 6; rule.LocalPorts = "8443";
            rule.Direction = 1; rule.Profiles = 2; rule.RemoteAddresses = "LocalSubnet"; rule.Action = 1; rule.EdgeTraversal = false; rule.Enabled = true;
            ((dynamic)rules!).Add(rule);
        }
        finally { foreach (var item in new[] { created, existing, rules, policy }) if (item != null && Marshal.IsComObject(item)) Marshal.FinalReleaseComObject(item); }
    }
}
