using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Application.Net.Remote;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Configuration;

/// <summary>Нормализация и сохранение конфигурации (включая проверку удалённого хоста).</summary>
public sealed class NetConfigurationApplyService
{
    private readonly NetConfigurationPersistenceService _persistence;
    private readonly NetRemoteHostPreConnectValidator _remotePreConnect;

    public NetConfigurationApplyService(
        NetConfigurationPersistenceService persistence,
        NetRemoteHostPreConnectValidator remotePreConnect
    )
    {
        _persistence = persistence;
        _remotePreConnect = remotePreConnect;
    }

    public async Task<Outcome<NetConfigurationState>> ApplyAsync(
        DiscoveryOptions transport,
        CancellationToken cancellationToken
    )
    {
        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(transport);

        Outcome<bool>? remoteValidateOutcome = await _remotePreConnect
            .ValidateIfClientConnectAsync(transport, cancellationToken)
            .ConfigureAwait(false);
        if (remoteValidateOutcome is { IsFailure: true } failedRemote)
            return Outcome<NetConfigurationState>.Fail(failedRemote.Error);

        return await _persistence
            .SaveAsync(transport, NetApiUserMessages.ConfigurationSaveFailed, cancellationToken)
            .ConfigureAwait(false);
    }
}
