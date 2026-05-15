using System.Net;
using System.Net.Sockets;

namespace DistributedLocalSystem.Core.NetDiscovery.Networking;

/// <summary>Сравнение IP-адресов с учётом IPv4-mapped IPv6.</summary>
public static class IpAddressEquality
{
    public static bool AreEquivalent(IPAddress a, IPAddress b)
    {
        if (a.Equals(b))
            return true;

        try
        {
            IPAddress na = Normalize(a);
            IPAddress nb = Normalize(b);
            return na.Equals(nb);
        }
        catch
        {
            return false;
        }
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6
            ? address.MapToIPv4()
            : address;
}
