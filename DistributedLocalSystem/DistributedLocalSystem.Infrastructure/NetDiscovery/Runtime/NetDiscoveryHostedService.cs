using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Старт/стоп discovery по <see cref="DiscoveryOptions.Role"/> из репозитория настроек.</summary>
public sealed class NetDiscoveryHostedService : IHostedService
{
    private readonly NetDiscoveryService _net;
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly ILogger<NetDiscoveryHostedService> _log;

    public NetDiscoveryHostedService(
        NetDiscoveryService net,
        INetDiscoverySettingsRepository settings,
        ILogger<NetDiscoveryHostedService> log
    )
    {
        _net = net;
        _settings = settings;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _settings.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        DiscoveryOptions opt = _settings.GetCurrent();
        switch (opt.ParsedRole)
        {
            case NetConfiguredRole.Host:
                _net.StartHost();
                _log.LogInformation("Net: configured role host — UDP beacon started");
                break;
            case NetConfiguredRole.Client:
                _net.StartClient();
                _log.LogInformation("Net: configured role client — discovery started");
                break;
            default:
                _log.LogInformation("Net: configured role none — discovery idle");
                break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _net.Stop();
        return Task.CompletedTask;
    }
}
