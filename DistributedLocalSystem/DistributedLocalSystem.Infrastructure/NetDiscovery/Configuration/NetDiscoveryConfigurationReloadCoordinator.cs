using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Configuration;

public sealed class NetDiscoveryConfigurationReloadCoordinator
    : INetDiscoveryConfigurationReloadCoordinator
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly INetDiscoveryRuntime _net;
    private readonly ILogger<NetDiscoveryConfigurationReloadCoordinator> _log;

    public NetDiscoveryConfigurationReloadCoordinator(
        INetDiscoverySettingsRepository settings,
        INetDiscoveryRuntime net,
        ILogger<NetDiscoveryConfigurationReloadCoordinator> log
    )
    {
        _settings = settings;
        _net = net;
        _log = log;
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        DiscoveryOptions next = await _settings
            .ReloadFromDatabaseAsync(cancellationToken)
            .ConfigureAwait(false);

        _net.RealignWithCurrentConfiguration();

        _log.LogInformation(
            "Net: configuration re-applied from database (Role={Role}, AppId={AppId})",
            next.Role,
            next.AppId
        );
    }
}
