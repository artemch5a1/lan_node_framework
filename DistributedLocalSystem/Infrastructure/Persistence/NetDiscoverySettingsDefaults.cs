using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence.Entities;

namespace DistributedLocalSystem.Core.Persistence;

/// <summary>Стартовая строка при первом создании БД (значения как в прежнем appsettings.json → Net).</summary>
public static class NetDiscoverySettingsDefaults
{
    public static NetDiscoverySettingsEntity CreateSeedEntity() =>
        new()
        {
            Id = NetDiscoverySettingsEntity.SingleRowId,
            Role = "client",
            AppId = "test-backend",
            UdpPort = 49000,
            LanPort = 17000,
            BeaconIntervalMs = 2000,
            DiscoveryTimeoutMs = 5000,
            ProtocolVersion = 1,
        };

    public static DiscoveryOptions ToDiscoveryOptions(NetDiscoverySettingsEntity e) =>
        new()
        {
            Role = e.Role,
            AppId = e.AppId,
            UdpPort = e.UdpPort,
            LanPort = e.LanPort,
            BeaconIntervalMs = e.BeaconIntervalMs,
            DiscoveryTimeoutMs = e.DiscoveryTimeoutMs,
            ProtocolVersion = e.ProtocolVersion,
        };

    public static DiscoveryOptions Clone(DiscoveryOptions o) =>
        new()
        {
            Role = o.Role,
            AppId = o.AppId,
            UdpPort = o.UdpPort,
            LanPort = o.LanPort,
            BeaconIntervalMs = o.BeaconIntervalMs,
            DiscoveryTimeoutMs = o.DiscoveryTimeoutMs,
            ProtocolVersion = o.ProtocolVersion,
        };
}
