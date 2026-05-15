using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Networking;
using DistributedLocalSystem.Infrastructure.Udp;
using Microsoft.Extensions.Logging;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Запуск режима хоста (UDP beacon).</summary>
internal sealed class NetDiscoveryHostModeStarter
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _localIdentity;
    private readonly LanHostCollisionPreflight _collisionPreflight;
    private readonly ILogger _log;

    public NetDiscoveryHostModeStarter(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity,
        ILogger log
    )
    {
        _settings = settings;
        _localIdentity = localIdentity;
        _collisionPreflight = new LanHostCollisionPreflight(settings, localIdentity, log);
        _log = log;
    }

    public void Start(
        NetDiscoveryLiveState live,
        ref CancellationTokenSource? runCts,
        ref Task? runTask,
        ref ApiUdpAnnouncer? hostAnnouncer
    )
    {
        DiscoveryOptions opt = _settings.GetCurrent();

        if (_collisionPreflight.AnotherHostAlreadyPresent(opt))
        {
            string error =
                $"Net: another host is already running in LAN for AppId '{opt.AppId}'. "
                + "This instance cannot start in host mode.";
            _log.LogError(error);
            throw new InvalidOperationException(error);
        }

        live.State = NetDiscoveryState.HostBeaconing;
        live.Peer.Clear();
        live.ThisHostIp = LocalLanIpv4Catalog.ResolvePrimaryLanIPv4();

        runCts = new CancellationTokenSource();
        CancellationToken token = runCts.Token;

        hostAnnouncer = new ApiUdpAnnouncer(_settings, _localIdentity);
        hostAnnouncer.StartAsync(token).GetAwaiter().GetResult();

        runTask = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            },
            token
        );

        _log.LogInformation("Net: host mode, UDP announcement started ({AppId})", opt.AppId);
    }
}
