using System.Net;
using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Infrastructure.Persistence;
using DistributedLocalSystem.Infrastructure.Persistence.Entities;

namespace DistributedLocalSystem.Core.Persistence;

/// <summary>Стартовая строка при первом создании БД (значения как в прежнем appsettings.json → Net).</summary>
public static class NetDiscoverySettingsDefaults
{
    public static NetDiscoverySettingsEntity CreateSeedEntity()
    {
        string instanceSlug = NetDiscoveryRowNormalizer.NewRandomInstanceSlug();
        string productSlug = "default";
        return new()
        {
            Id = NetDiscoverySettingsEntity.SingleRowId,
            Role = "host",
            ProductSlug = productSlug,
            InstanceSlug = instanceSlug,
            InstanceGuid = Guid.NewGuid().ToString("N"),
            AppId = LanBeaconName.Build(productSlug, instanceSlug),
            RemoteHostIp = null,
            UdpPort = 49000,
            LanPort = 17891,
            BeaconIntervalMs = 2000,
            DiscoveryTimeoutMs = 5000,
            ProtocolVersion = 1,
        };
    }

    /// <summary>Пересобирает <see cref="DiscoveryOptions.AppId"/> из slug’ов, если оба валидны.</summary>
    public static void SyncComputedAppId(DiscoveryOptions o)
    {
        if (LanBeaconName.IsValidSlug(o.ProductSlug) && LanBeaconName.IsValidSlug(o.InstanceSlug))
            o.AppId = LanBeaconName.Build(o.ProductSlug, o.InstanceSlug);
    }

    /// <summary>
    /// Роль не задаётся вручную: при валидном <see cref="DiscoveryOptions.RemoteHostIp"/> — client, иначе host.
    /// Некорректный IP сбрасывает удалённый хост.
    /// </summary>
    public static void ApplyRoleFromRemoteHost(DiscoveryOptions o)
    {
        string? ip = o.RemoteHostIp?.Trim();
        if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out _))
        {
            o.Role = "client";
            o.RemoteHostIp = ip;
            return;
        }

        o.RemoteHostIp = null;
        o.Role = "host";
    }

    public static DiscoveryOptions ToDiscoveryOptions(NetDiscoverySettingsEntity e) =>
        new()
        {
            Role = e.Role,
            AppId = e.AppId,
            ProductSlug = e.ProductSlug,
            InstanceSlug = e.InstanceSlug,
            InstanceGuid = e.InstanceGuid,
            RemoteHostIp = e.RemoteHostIp,
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
            ProductSlug = o.ProductSlug,
            InstanceSlug = o.InstanceSlug,
            InstanceGuid = o.InstanceGuid,
            RemoteHostIp = o.RemoteHostIp,
            UdpPort = o.UdpPort,
            LanPort = o.LanPort,
            BeaconIntervalMs = o.BeaconIntervalMs,
            DiscoveryTimeoutMs = o.DiscoveryTimeoutMs,
            ProtocolVersion = o.ProtocolVersion,
        };
}
