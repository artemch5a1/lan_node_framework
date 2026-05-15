using System.Diagnostics.CodeAnalysis;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Udp;
using Microsoft.Extensions.Logging;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Координация UDP discovery: делегирует запуск режимов узким компонентам.</summary>
public sealed class NetDiscoveryService : INetDiscoveryRuntime
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly ILogger<NetDiscoveryService> _log;
    private readonly object _gate = new();

    private readonly NetDiscoveryLiveState _live = new();
    private readonly NetDiscoveryBackgroundStopper _backgroundStopper;
    private readonly NetDiscoveryHostModeStarter _hostStarter;
    private readonly NetDiscoveryClientModeStarter _clientStarter;

    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private ApiUdpAnnouncer? _hostAnnouncer;
    private UdpDiscoveryService? _clientDiscovery;

    public NetDiscoveryService(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity,
        ILogger<NetDiscoveryService> log
    )
    {
        _settings = settings;
        _log = log;
        _backgroundStopper = new NetDiscoveryBackgroundStopper(log);
        _hostStarter = new NetDiscoveryHostModeStarter(settings, localIdentity, log);
        _clientStarter = new NetDiscoveryClientModeStarter(settings, localIdentity, log, _gate);
    }

    public void RealignWithCurrentConfiguration()
    {
        Stop();
        DiscoveryOptions snap = _settings.GetCurrent();
        switch (snap.ParsedRole)
        {
            case NetConfiguredRole.Host:
                StartHost();
                break;
            case NetConfiguredRole.Client:
                StartClient();
                break;
        }
    }

    public async Task<DiscoveryOptions> ChangeConfiguration(
        DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken = default
    ) => await _settings.UpdateConfiguration(newDiscoveryOptions, cancellationToken);

    public DiscoveryOptions GetCurrentConfiguration() => _settings.GetCurrent();

    public NetStatusDto GetStatus()
    {
        lock (_gate)
        {
            DiscoveryOptions opt = _settings.GetCurrent();
            return NetDiscoveryStatusSnapshotFactory.Create(
                opt,
                _live.State,
                _live.ThisHostIp,
                _live.Peer
            );
        }
    }

    public void StartHost()
    {
        lock (_gate)
        {
            StopUnsafe();
            _hostStarter.Start(_live, ref _runCts, ref _runTask, ref _hostAnnouncer);
        }
    }

    public void StartClient()
    {
        lock (_gate)
        {
            StopUnsafe();
            _clientStarter.Start(_live, ref _runCts, ref _runTask, ref _clientDiscovery);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopUnsafe();
            _live.State = NetDiscoveryState.Idle;
            _live.Peer.Clear();
        }
    }

    public void RestartClientDiscoveryAfterRemoteHostFailure()
    {
        DiscoveryOptions snap = _settings.GetCurrent();
        if (snap.ParsedRole != NetConfiguredRole.Client)
            return;

        _log.LogWarning(
            "Net: remote host considered dead; restarting UDP discovery ({AppId})",
            snap.AppId
        );
        StartClient();
    }

    public bool TryGetHostProxyBaseUrl([NotNullWhen(true)] out string? baseUrl)
    {
        lock (_gate)
        {
            if (_live.State != NetDiscoveryState.ClientConnected)
            {
                baseUrl = null;
                return false;
            }

            baseUrl = _live.Peer.BuildBaseUrl();
            return !string.IsNullOrEmpty(baseUrl);
        }
    }

    private void StopUnsafe() =>
        _backgroundStopper.Stop(
            ref _runCts,
            ref _runTask,
            ref _hostAnnouncer,
            ref _clientDiscovery
        );

    public void Dispose() => Stop();
}
