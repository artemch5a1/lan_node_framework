using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

public sealed class DiscoveryOptions
{
    /// <summary>Логическое имя группы (хранится в SQLite, таблица net_discovery_settings).</summary>
    public const string SectionName = "Net";

    /// <summary>Режим из хранилища: host | client (none устарел; при сохранении выводится из RemoteHostIp).</summary>
    public string Role { get; set; } = "host";

    /// <summary>Полное имя в UDP beacon: <c>DLSv1-product-instance</c> (см. <see cref="LanBeaconName"/>).</summary>
    public string AppId { get; set; } = "";

    /// <summary>Общий slug продукта для всех экземпляров линейки.</summary>
    public string ProductSlug { get; set; } = "";

    /// <summary>Slug экземпляра (уникальность в LAN — best-effort).</summary>
    public string InstanceSlug { get; set; } = "";

    /// <summary>Стабильный идентификатор экземпляра (не участвует в beacon v1).</summary>
    public string InstanceGuid { get; set; } = "";

    /// <summary>Для режима client: явный LAN IP хоста (без UDP discovery).</summary>
    public string? RemoteHostIp { get; set; }
    public int UdpPort { get; set; } = 49152;
    public int LanPort { get; set; } = 17891;
    public int BeaconIntervalMs { get; set; } = 2000;
    public int DiscoveryTimeoutMs { get; set; } = 5000;
    public int ProtocolVersion { get; set; } = 1;

    public static NetConfiguredRole ParseRole(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            "host" => NetConfiguredRole.Host,
            "client" => NetConfiguredRole.Client,
            _ => NetConfiguredRole.None,
        };

    public NetConfiguredRole ParsedRole => ParseRole(Role);

    /// <summary>Неглубокая копия полей для безопасной мутации без изменения исходного снимка.</summary>
    public DiscoveryOptions Clone() =>
        new()
        {
            Role = Role,
            AppId = AppId,
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
}

public enum NetConfiguredRole
{
    None,
    Host,
    Client,
}

public static class NetRoleApi
{
    public static string Format(NetConfiguredRole r) =>
        r switch
        {
            NetConfiguredRole.Host => "host",
            NetConfiguredRole.Client => "client",
            _ => "none",
        };
}
