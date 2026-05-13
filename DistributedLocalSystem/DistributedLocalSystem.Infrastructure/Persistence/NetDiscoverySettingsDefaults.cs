using System.Net;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Persistence.Entities;

namespace DistributedLocalSystem.Infrastructure.Persistence;

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
            RemoteHostIp = null,
            UdpPort = 49000,
            LanPort = 17891,
            BeaconIntervalMs = 2000,
            DiscoveryTimeoutMs = 5000,
            ProtocolVersion = 1,
        };
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

    public static DiscoveryOptions Clone(DiscoveryOptions o) => o.Clone();
}
