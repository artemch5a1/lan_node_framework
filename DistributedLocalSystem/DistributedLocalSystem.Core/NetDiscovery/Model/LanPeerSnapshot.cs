using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Узел в списке LAN: из UDP или «липкая» строка текущего подключения без beacon.</summary>
public sealed record LanPeerSnapshot(
    string IpAddress,
    string ProductSlug,
    string InstanceSlug,
    /// <summary><see langword="false"/> — узел не виден в эфире, но к нему есть сохранённое подключение (client).</summary>
    bool SeenInDiscovery = true
)
{
    /// <summary>Полное имя в эфире; для «липкого» узла без валидной пары slug — «—».</summary>
    public string BeaconName =>
        LanBeaconName.FormatFullNameOrDash(ProductSlug, InstanceSlug);
}
