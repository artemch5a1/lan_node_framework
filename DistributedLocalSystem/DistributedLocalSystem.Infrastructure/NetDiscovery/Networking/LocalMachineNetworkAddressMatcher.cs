using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Networking;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Networking;

/// <summary>Проверка, указывает ли адрес на этот компьютер.</summary>
public sealed class LocalMachineNetworkAddressMatcher : ILocalMachineAddressMatcher
{
    public bool IsLocalMachine(IPAddress target, string? reportedPrimaryHostIp)
    {
        if (IPAddress.IsLoopback(target))
            return true;

        if (
            !string.IsNullOrWhiteSpace(reportedPrimaryHostIp)
            && IPAddress.TryParse(reportedPrimaryHostIp.Trim(), out IPAddress? reported)
            && reported is not null
            && IpAddressEquality.AreEquivalent(target, reported)
        )
            return true;

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation u in ni.GetIPProperties().UnicastAddresses)
            {
                if (
                    u.Address.AddressFamily
                    is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                )
                    continue;

                if (IpAddressEquality.AreEquivalent(target, u.Address))
                    return true;
            }
        }

        return false;
    }
}
