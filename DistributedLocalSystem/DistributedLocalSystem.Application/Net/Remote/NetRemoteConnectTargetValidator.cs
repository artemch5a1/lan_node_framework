using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Remote;

/// <summary>Проверка совместимости удалённой конфигурации для подключения клиента.</summary>
public static class NetRemoteConnectTargetValidator
{
    public static Outcome<bool> ValidateRole(DiscoveryOptions remoteConfiguration)
    {
        if (
            NetRemoteConnectionValidation.TryValidateRemoteConnectTarget(
                remoteConfiguration,
                out string? userMessage
            )
        )
            return Outcome<bool>.Ok(true);

        return Outcome<bool>.Fail(
            NetFlowErrorCodes.RemoteHostIsClient,
            userMessage ?? NetRemoteConnectionValidation.RemoteIsClientMessage
        );
    }

    public static Outcome<bool> ValidateProductSlug(
        DiscoveryOptions localConfiguration,
        DiscoveryOptions remoteConfiguration
    )
    {
        string localProduct = localConfiguration.ProductSlug.Trim();
        string remoteProduct = (remoteConfiguration.ProductSlug ?? "").Trim();
        if (string.Equals(localProduct, remoteProduct, StringComparison.OrdinalIgnoreCase))
            return Outcome<bool>.Ok(true);

        return Outcome<bool>.Fail(
            NetFlowErrorCodes.InvalidConfiguration,
            "Product slug на удалённом узле не совпадает с вашим. Это другая линейка продукта."
        );
    }

    public static Outcome<bool> ValidateInstanceGuid(
        DiscoveryOptions localConfiguration,
        DiscoveryOptions remoteConfiguration
    )
    {
        string remoteGuid = remoteConfiguration.InstanceGuid?.Trim() ?? "";
        if (string.IsNullOrEmpty(remoteGuid))
        {
            return Outcome<bool>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                "Ответ узла не содержит InstanceGuid — похоже, это не API этой программы."
            );
        }

        string localGuid = localConfiguration.InstanceGuid?.Trim() ?? "";
        if (
            localGuid.Length > 0
            && string.Equals(localGuid, remoteGuid, StringComparison.OrdinalIgnoreCase)
        )
        {
            return Outcome<bool>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Ответ с указанного адреса относится к этому же экземпляру (совпадает InstanceGuid). Укажите другой компьютер."
            );
        }

        return Outcome<bool>.Ok(true);
    }

    public static LanPeerSnapshot BuildPeerSnapshot(
        string canonicalIp,
        DiscoveryOptions remoteConfiguration
    )
    {
        string remoteProduct = (remoteConfiguration.ProductSlug ?? "").Trim();
        string remoteSlug = remoteConfiguration.InstanceSlug?.Trim() ?? "";
        string instanceLabel = LanBeaconName.IsValidSlug(remoteSlug)
            ? remoteSlug
            : "удалённый узел";

        return new LanPeerSnapshot(
            canonicalIp,
            remoteProduct,
            instanceLabel,
            SeenInDiscovery: false
        );
    }
}
