using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Networking;

/// <summary>Перечисление локальных IPv4-адресов и выбор «основного».</summary>
public static class LocalLanIpv4Catalog
{
    public static string? ResolvePrimaryLanIPv4()
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress a in host.AddressList)
            {
                if (!IsUsableLanIPv4(a))
                    continue;
                return a.ToString();
            }
        }
        catch
        {
            // caller logs if needed
        }

        return null;
    }

    public static IReadOnlyList<NetLocalIpv4Endpoint> EnumerateEndpoints(
        string? primaryAddressFirst
    )
    {
        try
        {
            List<NetLocalIpv4Endpoint> items = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                string label =
                    !string.IsNullOrWhiteSpace(ni.Description) ? ni.Description.Trim()
                    : !string.IsNullOrWhiteSpace(ni.Name) ? ni.Name.Trim()
                    : "Сетевой адаптер";

                foreach (UnicastIPAddressInformation u in ni.GetIPProperties().UnicastAddresses)
                {
                    if (!IsUsableLanIPv4(u.Address))
                        continue;

                    string addr = u.Address.ToString();
                    if (!seen.Add(addr))
                        continue;

                    items.Add(new NetLocalIpv4Endpoint(addr, label));
                }
            }

            PromotePrimaryToFront(items, primaryAddressFirst);
            return items;
        }
        catch
        {
            return Array.Empty<NetLocalIpv4Endpoint>();
        }
    }

    private static void PromotePrimaryToFront(
        List<NetLocalIpv4Endpoint> items,
        string? primaryAddressFirst
    )
    {
        if (string.IsNullOrWhiteSpace(primaryAddressFirst))
            return;

        int idx = items.FindIndex(e =>
            string.Equals(e.Address, primaryAddressFirst, StringComparison.OrdinalIgnoreCase)
        );
        if (idx <= 0)
            return;

        NetLocalIpv4Endpoint pick = items[idx];
        items.RemoveAt(idx);
        items.Insert(0, pick);
    }

    public static bool IsUsableLanIPv4(IPAddress a) =>
        a.AddressFamily == AddressFamily.InterNetwork
        && !IPAddress.IsLoopback(a)
        && !a.ToString().StartsWith("169.254.", StringComparison.Ordinal);
}
