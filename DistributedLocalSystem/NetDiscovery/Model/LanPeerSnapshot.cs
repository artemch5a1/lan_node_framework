namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>Узел в списке LAN: из UDP или «липкая» строка текущего подключения без beacon.</summary>
public sealed record LanPeerSnapshot(
    string IpAddress,
    string BeaconName,
    string ProductSlug,
    string InstanceSlug,
    /// <summary><see langword="false"/> — узел не виден в эфире, но к нему есть сохранённое подключение (client).</summary>
    bool SeenInDiscovery = true
);
