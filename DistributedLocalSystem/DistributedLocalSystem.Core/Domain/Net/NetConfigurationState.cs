using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Domain.Net;

/// <summary>Непрозрачная снимок конфигурации сети (без деталей хранения и discovery-стратегии).</summary>
public sealed record NetConfigurationState(
    string Role,
    string ProductSlug,
    string InstanceSlug,
    string InstanceGuid,
    string? RemoteHostIp,
    int UdpPort,
    int LanPort,
    int BeaconIntervalMs,
    int DiscoveryTimeoutMs,
    int ProtocolVersion
)
{
    public string AppId => LanBeaconName.FormatFullNameOrEmpty(ProductSlug, InstanceSlug);

    public static NetConfigurationState FromTransport(DiscoveryOptions o) =>
        new(
            o.Role,
            o.ProductSlug,
            o.InstanceSlug,
            o.InstanceGuid,
            o.RemoteHostIp,
            o.UdpPort,
            o.LanPort,
            o.BeaconIntervalMs,
            o.DiscoveryTimeoutMs,
            o.ProtocolVersion
        );

    public DiscoveryOptions ToTransport()
    {
        DiscoveryOptions t = new()
        {
            Role = Role,
            ProductSlug = ProductSlug,
            InstanceSlug = InstanceSlug,
            InstanceGuid = InstanceGuid,
            RemoteHostIp = RemoteHostIp,
            UdpPort = UdpPort,
            LanPort = LanPort,
            BeaconIntervalMs = BeaconIntervalMs,
            DiscoveryTimeoutMs = DiscoveryTimeoutMs,
            ProtocolVersion = ProtocolVersion,
        };
        return t;
    }
}
