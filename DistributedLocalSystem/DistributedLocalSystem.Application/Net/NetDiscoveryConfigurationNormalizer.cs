using System.Net;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net;

/// <summary>
/// Нормализация режима и полей конфигурации перед сохранением (раньше вызывалось из Infrastructure).
/// </summary>
public static class NetDiscoveryConfigurationNormalizer
{
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
}
