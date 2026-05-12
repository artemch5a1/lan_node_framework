using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Domain.Net;

/// <summary>Снимок состояния сети для наблюдателей (режим, адреса, идентификаторы).</summary>
public sealed record NetRuntimeSnapshot(
    string ConfiguredRole,
    NetDiscoveryState State,
    string? ThisHostIp,
    string? RemoteHostIp,
    int? RemoteTcpPort,
    string? RemoteHostBaseUrl,
    int LanPort,
    int UdpPort,
    string AppId,
    string ProductSlug,
    string InstanceSlug,
    string InstanceGuid
)
{
    public static NetRuntimeSnapshot FromTransport(NetStatusDto d) =>
        new(
            d.ConfiguredRole,
            d.State,
            d.ThisHostIp,
            d.RemoteHostIp,
            d.RemoteTcpPort,
            d.RemoteHostBaseUrl,
            d.LanPort,
            d.UdpPort,
            d.AppId,
            d.ProductSlug,
            d.InstanceSlug,
            d.InstanceGuid
        );

    public NetStatusDto ToTransport() =>
        new(
            ConfiguredRole,
            State,
            ThisHostIp,
            RemoteHostIp,
            RemoteTcpPort,
            RemoteHostBaseUrl,
            LanPort,
            UdpPort,
            AppId,
            ProductSlug,
            InstanceSlug,
            InstanceGuid
        );
}
