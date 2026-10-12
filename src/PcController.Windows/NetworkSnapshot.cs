using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PcController.Windows;

public sealed record NetworkSnapshot(IPAddress[] PrivateAddresses, IPAddress[] AllAddresses, string Notice)
{
    // GetAdapterId returns a native GUID, which IDispatch/dynamic cannot marshal.
    // Use the SDK's dual-interface vtable for this call instead of automation.
    [ComImport, Guid("DCB00005-570F-4A9B-8D69-199FDBA5723B"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    private interface INetworkConnection
    {
        [return: MarshalAs(UnmanagedType.Interface)] object GetNetwork();
        [return: MarshalAs(UnmanagedType.VariantBool)] bool get_IsConnectedToInternet();
        [return: MarshalAs(UnmanagedType.VariantBool)] bool get_IsConnected();
        int GetConnectivity();
        void GetConnectionId(out Guid id);
        void GetAdapterId(out Guid id);
        int GetDomainType();
    }

    public static NetworkSnapshot Read()
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToArray();
        static IEnumerable<IPAddress> Addresses(NetworkInterface adapter) => adapter.GetIPProperties().UnicastAddresses
            .Select(a => a.Address).Where(a => a.AddressFamily == AddressFamily.InterNetwork &&
                !a.ToString().StartsWith("169.254.", StringComparison.Ordinal));
        var all = adapters.SelectMany(Addresses).Distinct().ToArray();
        var trusted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        object? manager = null;
        try
        {
            manager = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B"), true)!);
            dynamic list = manager!;
            foreach (object rawNetwork in list.GetNetworks(1))
            {
                dynamic network = rawNetwork;
                try
                {
                // Private only. Domain networks require organizational policy rather than implicit exposure.
                if ((int)network.GetCategory() != 1) continue;
                foreach (object connection in network.GetNetworkConnections())
                {
                    try
                    {
                        ((INetworkConnection)connection).GetAdapterId(out var adapterId);
                        trusted.Add(adapterId.ToString());
                    }
                    finally { if (Marshal.IsComObject(connection)) Marshal.FinalReleaseComObject(connection); }
                }
                }
                finally { if (Marshal.IsComObject(rawNetwork)) Marshal.FinalReleaseComObject(rawNetwork); }
            }
        }
        catch (Exception exception) when (exception is COMException or ArgumentException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            return new([], all, "Cannot identify Private networks. Serving loopback only; check Windows network settings. Retrying automatically.");
        }
        finally { if (manager != null && Marshal.IsComObject(manager)) Marshal.FinalReleaseComObject(manager); }
        var local = adapters.Where(a => trusted.Contains(a.Id.Trim('{', '}'))).SelectMany(Addresses).Distinct().ToArray();
        return new(local, all, local.Length == 0
            ? "No Private network found. Serving loopback only. If this is your trusted home LAN, set its Windows network profile to Private and add the scoped firewall rule. The address refreshes automatically."
            : "Private LAN detected. Phone reachability still requires the scoped firewall rule and a trusted certificate.");
    }
}
