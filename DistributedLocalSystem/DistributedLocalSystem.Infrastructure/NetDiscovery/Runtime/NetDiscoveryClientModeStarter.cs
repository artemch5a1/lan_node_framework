using System.Net;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Networking;
using DistributedLocalSystem.Infrastructure.Udp;
using Microsoft.Extensions.Logging;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Запуск режима клиента (фиксированный IP или UDP discovery).</summary>
internal sealed class NetDiscoveryClientModeStarter
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _localIdentity;
    private readonly ILogger _log;
    private readonly object _gate;

    public NetDiscoveryClientModeStarter(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity,
        ILogger log,
        object gate
    )
    {
        _settings = settings;
        _localIdentity = localIdentity;
        _log = log;
        _gate = gate;
    }

    public void Start(
        NetDiscoveryLiveState live,
        ref CancellationTokenSource? runCts,
        ref Task? runTask,
        ref UdpDiscoveryService? clientDiscovery
    )
    {
        DiscoveryOptions opt = _settings.GetCurrent();

        live.State = NetDiscoveryState.ClientDiscovering;
        live.Peer.Clear();
        live.ThisHostIp = LocalLanIpv4Catalog.ResolvePrimaryLanIPv4();

        if (TryConnectFixedRemote(opt, live))
            return;

        StartUdpDiscovery(opt, live, ref runCts, ref runTask, ref clientDiscovery);
    }

    private bool TryConnectFixedRemote(DiscoveryOptions opt, NetDiscoveryLiveState live)
    {
        string? fixedIp = opt.RemoteHostIp?.Trim();
        if (
            string.IsNullOrEmpty(fixedIp)
            || !IPAddress.TryParse(fixedIp, out IPAddress? parsedAddr)
            || parsedAddr is null
        )
            return false;

        live.State = NetDiscoveryState.ClientConnected;
        live.Peer.Assign(fixedIp, opt.LanPort);
        _log.LogInformation(
            "Net: client mode, fixed remote host {Host}:{Tcp} (no UDP discovery)",
            fixedIp,
            opt.LanPort
        );
        return true;
    }

    private void StartUdpDiscovery(
        DiscoveryOptions opt,
        NetDiscoveryLiveState live,
        ref CancellationTokenSource? runCts,
        ref Task? runTask,
        ref UdpDiscoveryService? clientDiscovery
    )
    {
        runCts = new CancellationTokenSource();
        CancellationToken token = runCts.Token;

        UdpDiscoveryService discovery = new(
            _settings,
            _localIdentity,
            LanUdpPeerFilterKind.SameProductSlug
        );
        clientDiscovery = discovery;

        TaskCompletionSource<DiscoveredServer> tcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        discovery.ServerDiscovered += server => tcs.TrySetResult(server);

        int lanPort = opt.LanPort;
        string appId = opt.AppId;

        runTask = Task.Run(
            async () =>
            {
                try
                {
                    await discovery.StartAsync(token).ConfigureAwait(false);

                    DiscoveredServer server = await tcs.Task.WaitAsync(token).ConfigureAwait(false);

                    lock (_gate)
                    {
                        live.State = NetDiscoveryState.ClientConnected;
                        live.Peer.Assign(server.IpAddress.ToString(), lanPort);
                    }

                    _log.LogInformation(
                        "Net: host found at {Host}:{Tcp} ({AppId})",
                        server.IpAddress,
                        lanPort,
                        appId
                    );

                    await discovery.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Net: client discovery error");
                }
            },
            token
        );

        _log.LogInformation("Net: client mode, listening UDP until host found ({AppId})", appId);
    }
}
