namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>Узел, обнаруженный при сканировании LAN.</summary>
public sealed record LanPeerSnapshot(
    string IpAddress,
    string BeaconName,
    string ProductSlug,
    string InstanceSlug
);
