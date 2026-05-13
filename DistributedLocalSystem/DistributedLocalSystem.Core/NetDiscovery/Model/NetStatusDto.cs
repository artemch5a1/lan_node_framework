using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

public sealed record NetStatusDto(
    string ConfiguredRole,
    NetDiscoveryState State,
    string? ThisHostIp,
    string? RemoteHostIp,
    int? RemoteTcpPort,
    string? RemoteHostBaseUrl,
    int LanPort,
    int UdpPort,
    string ProductSlug,
    string InstanceSlug,
    string InstanceGuid
)
{
    /// <summary>Полное beacon-имя; не хранится отдельно.</summary>
    public string AppId => LanBeaconName.FormatFullNameOrEmpty(ProductSlug, InstanceSlug);
}
