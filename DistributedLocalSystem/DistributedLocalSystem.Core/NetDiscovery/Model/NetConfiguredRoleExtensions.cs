using System.ComponentModel;
using System.Reflection;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Парсинг, сериализация и человекочитаемые подписи <see cref="NetConfiguredRole"/>.</summary>
public static class NetConfiguredRoleExtensions
{
    public static bool TryParseApiString(string? raw, out NetConfiguredRole role)
    {
        role = raw?.Trim().ToLowerInvariant() switch
        {
            "host" => NetConfiguredRole.Host,
            "client" => NetConfiguredRole.Client,
            _ => NetConfiguredRole.None,
        };
        return role != NetConfiguredRole.None || string.Equals(raw?.Trim(), "none", StringComparison.OrdinalIgnoreCase);
    }

    public static NetConfiguredRole ParseApiString(string? raw) =>
        TryParseApiString(raw, out NetConfiguredRole role) ? role : NetConfiguredRole.None;

    public static string ToApiString(this NetConfiguredRole role) =>
        role switch
        {
            NetConfiguredRole.Host => "host",
            NetConfiguredRole.Client => "client",
            _ => "none",
        };

    /// <summary>Русскоязычное имя из <see cref="DescriptionAttribute"/>.</summary>
    public static string GetDescription(this NetConfiguredRole role)
    {
        string? fromAttribute = typeof(NetConfiguredRole)
            .GetField(role.ToString())
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description;

        return string.IsNullOrWhiteSpace(fromAttribute) ? role.ToString() : fromAttribute;
    }

    public static bool IsClientRole(this NetConfiguredRole role) => role == NetConfiguredRole.Client;

    /// <summary>Можно ли подключаться к этому экземпляру как к LAN-хосту.</summary>
    public static bool CanAcceptIncomingLanConnection(this NetConfiguredRole role) =>
        role != NetConfiguredRole.Client;
}
