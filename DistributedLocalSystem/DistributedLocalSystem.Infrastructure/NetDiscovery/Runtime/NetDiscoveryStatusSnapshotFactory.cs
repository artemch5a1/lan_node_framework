using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Networking;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Сборка <see cref="NetStatusDto"/> из runtime-состояния.</summary>
internal static class NetDiscoveryStatusSnapshotFactory
{
    public static NetStatusDto Create(
        DiscoveryOptions opt,
        NetDiscoveryState state,
        string? thisHostIp,
        NetDiscoveryPeerBinding peer
    ) =>
        new(
            ConfiguredRole: opt.ParsedRole.ToApiString(),
            State: state,
            ThisHostIp: thisHostIp,
            RemoteHostIp: peer.RemoteHostIp,
            RemoteTcpPort: peer.RemoteTcpPort,
            RemoteHostBaseUrl: peer.BuildBaseUrl(),
            LanPort: opt.LanPort,
            UdpPort: opt.UdpPort,
            ProductSlug: opt.ProductSlug,
            InstanceSlug: opt.InstanceSlug,
            InstanceGuid: opt.InstanceGuid,
            LocalIpv4Endpoints: LocalLanIpv4Catalog.EnumerateEndpoints(thisHostIp)
        );
}
