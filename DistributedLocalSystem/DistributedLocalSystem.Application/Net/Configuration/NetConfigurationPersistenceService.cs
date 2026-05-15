using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Configuration;

/// <summary>Сохранение конфигурации сети и перезагрузка discovery.</summary>
public sealed class NetConfigurationPersistenceService
{
    private readonly INetDiscoveryRuntime _net;
    private readonly INetDiscoveryConfigurationReloadCoordinator _reloadCoordinator;

    public NetConfigurationPersistenceService(
        INetDiscoveryRuntime net,
        INetDiscoveryConfigurationReloadCoordinator reloadCoordinator
    )
    {
        _net = net;
        _reloadCoordinator = reloadCoordinator;
    }

    public async Task<Outcome<NetConfigurationState>> SaveAsync(
        DiscoveryOptions transport,
        string configurationUpdateErrorMessage,
        CancellationToken cancellationToken
    )
    {
        if (!NetDiscoveryInputValidation.TryValidatePersist(transport, out NetFlowError? ve))
            return Outcome<NetConfigurationState>.Fail(ve!);

        try
        {
            DiscoveryOptions updated = await _net.ChangeConfiguration(transport, cancellationToken)
                .ConfigureAwait(false);
            await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
            return Outcome<NetConfigurationState>.Ok(NetConfigurationState.FromTransport(updated));
        }
        catch (InvalidOperationException ex)
        {
            if (HostCollisionExceptionClassifier.IsAnotherHostPresent(ex.Message))
                return Outcome<NetConfigurationState>.Fail(
                    new AnotherHostAlreadyPresentFault().ToFlowError()
                );

            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.HostCollision,
                NetApiUserMessages.HostRoleConflict
            );
        }
        catch (Exception)
        {
            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.ConfigurationUpdate,
                configurationUpdateErrorMessage
            );
        }
    }
}
