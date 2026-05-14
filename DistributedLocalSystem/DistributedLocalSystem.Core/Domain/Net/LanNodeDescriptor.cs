using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Domain.Net;

/// <summary>Узел в LAN с точки зрения домена (без источника: UDP, sticky и т.д.).</summary>
public sealed record LanNodeDescriptor(
    string IpAddress,
    string ProductSlug,
    string InstanceSlug,
    bool SeenInDiscovery
)
{
    public string BeaconName => LanBeaconName.FormatFullNameOrDash(ProductSlug, InstanceSlug);

    public static LanNodeDescriptor FromTransport(LanPeerSnapshot s) =>
        new(s.IpAddress, s.ProductSlug, s.InstanceSlug, s.SeenInDiscovery);

    public LanPeerSnapshot ToTransport() =>
        new(IpAddress, ProductSlug, InstanceSlug, SeenInDiscovery);
}
