using System.Diagnostics.CodeAnalysis;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Правила подключения к удалённому узлу по LAN.</summary>
public static class NetRemoteConnectionValidation
{
    public const string RemoteIsClientMessage =
        "Удалённый компьютер уже работает как клиент (подключён к другому узлу). "
        + "Подключиться к нему нельзя — выберите компьютер в режиме хоста.";

    public static bool TryValidateRemoteConnectTarget(
        DiscoveryOptions remoteConfiguration,
        [NotNullWhen(false)] out string? userMessage
    )
    {
        if (remoteConfiguration.ParsedRole.IsClientRole())
        {
            userMessage = RemoteIsClientMessage;
            return false;
        }

        userMessage = null;
        return true;
    }
}
