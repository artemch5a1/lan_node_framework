using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using DistributedLocalSystem.Core.Persistence.Abstractions;
using DistributedLocalSystem.Core.Udp;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>UDP discovery: хост — beacon, клиент — поиск хоста или <see cref="NetDiscoveryState.ClientLocalOnly"/>.</summary>
public sealed class NetDiscoveryService : IDisposable
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _localIdentity;
    private readonly ILogger<NetDiscoveryService> _log;
    private readonly object _gate = new();

    private CancellationTokenSource? _runCts;
    private Task? _runTask;

    private ApiUdpAnnouncer? _hostAnnouncer;
    private UdpDiscoveryService? _clientDiscovery;

    private NetDiscoveryState _state = NetDiscoveryState.Idle;
    private string? _remoteHostIp;
    private int? _remoteTcpPort;
    private string? _thisHostIp;

    public NetDiscoveryService(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity,
        ILogger<NetDiscoveryService> log
    )
    {
        _settings = settings;
        _localIdentity = localIdentity;
        _log = log;
    }

    /// <summary>
    /// Останавливает discovery и поднимает режим по актуальным настройкам из репозитория
    /// (после <see cref="INetDiscoveryConfigurationReloadCoordinator.ReloadAsync"/>).
    /// </summary>
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
            default:
                break;
        }
    }

    /// <summary>Снимок для <c>GET /api/net/status</c>.</summary>
    public NetStatusDto GetStatus()
    {
        lock (_gate)
        {
            DiscoveryOptions opt = _settings.GetCurrent();
            return new NetStatusDto(
                ConfiguredRole: NetRoleApi.Format(opt.ParsedRole),
                State: _state,
                ThisHostIp: _thisHostIp,
                RemoteHostIp: _remoteHostIp,
                RemoteTcpPort: _remoteTcpPort,
                RemoteHostBaseUrl: BuildRemoteBaseUrl(),
                LanPort: opt.LanPort,
                UdpPort: opt.UdpPort,
                AppId: opt.AppId
            );
        }
    }

    /// <summary>Режим хоста: периодический UDP beacon.</summary>
    public void StartHost()
    {
        lock (_gate)
        {
            DiscoveryOptions opt = _settings.GetCurrent();
            StopUnsafe();

            bool hostAlreadyExists = DetectExistingHostBeforeBeaconStart(opt);
            if (hostAlreadyExists)
            {
                string error =
                    $"Net: another host is already running in LAN for AppId '{opt.AppId}'. "
                    + "This instance cannot start in host mode.";
                _log.LogError(error);
                throw new InvalidOperationException(error);
            }

            _state = NetDiscoveryState.HostBeaconing;
            ClearRemotePeer();
            _thisHostIp = GetPrimaryLanIPv4();

            _runCts = new CancellationTokenSource();
            CancellationToken token = _runCts.Token;

            _hostAnnouncer = new ApiUdpAnnouncer(_settings, _localIdentity);
            _hostAnnouncer.StartAsync(token).GetAwaiter().GetResult();

            _runTask = Task.Run(
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

    private bool DetectExistingHostBeforeBeaconStart(DiscoveryOptions opt)
    {
        using UdpDiscoveryService probe = new(_settings, _localIdentity);
        using CancellationTokenSource timeoutCts = new(
            TimeSpan.FromMilliseconds(opt.DiscoveryTimeoutMs)
        );

        TaskCompletionSource<DiscoveredServer> tcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        probe.ServerDiscovered += server => tcs.TrySetResult(server);

        try
        {
            probe.StartAsync(timeoutCts.Token).GetAwaiter().GetResult();
            DiscoveredServer discovered = tcs
                .Task.WaitAsync(timeoutCts.Token)
                .GetAwaiter()
                .GetResult();
            _log.LogInformation(
                "Net: existing host detected before host startup at {Host}:{Tcp} ({AppId})",
                discovered.IpAddress,
                opt.LanPort,
                opt.AppId
            );
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Net: host preflight discovery failed, continuing host startup");
            return false;
        }
        finally
        {
            try
            {
                probe.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { }
        }
    }

    /// <summary>Режим клиента: поиск до таймаута, иначе <see cref="NetDiscoveryState.ClientLocalOnly"/>.</summary>
    public void StartClient()
    {
        lock (_gate)
        {
            DiscoveryOptions opt = _settings.GetCurrent();
            StopUnsafe();

            _state = NetDiscoveryState.ClientDiscovering;
            ClearRemotePeer();
            _thisHostIp = GetPrimaryLanIPv4();

            _runCts = new CancellationTokenSource();
            CancellationToken token = _runCts.Token;

            UdpDiscoveryService discovery = new(_settings, _localIdentity);
            _clientDiscovery = discovery;

            TaskCompletionSource<DiscoveredServer> tcs = new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            discovery.ServerDiscovered += server => tcs.TrySetResult(server);

            int lanPort = opt.LanPort;
            string appId = opt.AppId;

            _runTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await discovery.StartAsync(token).ConfigureAwait(false);

                        DiscoveredServer server = await tcs
                            .Task.WaitAsync(token)
                            .ConfigureAwait(false);

                        lock (_gate)
                        {
                            _state = NetDiscoveryState.ClientConnected;
                            _remoteHostIp = server.IpAddress.ToString();
                            _remoteTcpPort = lanPort;
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

            _log.LogInformation(
                "Net: client mode, listening UDP until host found ({AppId})",
                opt.AppId
            );
        }
    }

    /// <summary>Стоп фоновой задачи, состояние <see cref="NetDiscoveryState.Idle"/>.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            StopUnsafe();
            _state = NetDiscoveryState.Idle;
            ClearRemotePeer();
        }
    }

    private string? BuildRemoteBaseUrl()
    {
        if (string.IsNullOrEmpty(_remoteHostIp) || _remoteTcpPort is null or <= 0)
            return null;
        return $"http://{_remoteHostIp}:{_remoteTcpPort}";
    }

    /// <summary>
    /// Удалённый хост не ответил на проверку <c>/health</c> после сбоя прокси — сброс связи и снова UDP discovery (только режим client).
    /// </summary>
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

    /// <summary>
    /// Базовый URL LAN-хоста для проксирования HTTP (только <see cref="NetDiscoveryState.ClientConnected"/>).
    /// </summary>
    public bool TryGetHostProxyBaseUrl([NotNullWhen(true)] out string? baseUrl)
    {
        lock (_gate)
        {
            if (_state != NetDiscoveryState.ClientConnected)
            {
                baseUrl = null;
                return false;
            }

            baseUrl = BuildRemoteBaseUrl();
            return !string.IsNullOrEmpty(baseUrl);
        }
    }

    private void ClearRemotePeer()
    {
        _remoteHostIp = null;
        _remoteTcpPort = null;
    }

    private void StopUnsafe()
    {
        using CancellationTokenSource stopCts = new(TimeSpan.FromSeconds(2));
        CancellationToken stopToken = stopCts.Token;

        try
        {
            _runCts?.Cancel();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Net: Cancel on shutdown");
        }

        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Net: background task wait on shutdown");
        }

        try
        {
            _hostAnnouncer?.StopAsync(stopToken).GetAwaiter().GetResult();
        }
        catch { }
        finally
        {
            _hostAnnouncer?.Dispose();
            _hostAnnouncer = null;
        }

        try
        {
            _clientDiscovery?.StopAsync(stopToken).GetAwaiter().GetResult();
        }
        catch { }
        finally
        {
            _clientDiscovery?.Dispose();
            _clientDiscovery = null;
        }

        _runCts?.Dispose();
        _runCts = null;
        _runTask = null;
    }

    private string? GetPrimaryLanIPv4()
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress a in host.AddressList)
            {
                if (!IsUsableLanIPv4(a))
                    continue;
                return a.ToString();
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Net: could not resolve primary LAN IPv4");
        }

        return null;
    }

    private static bool IsUsableLanIPv4(IPAddress a) =>
        a.AddressFamily == AddressFamily.InterNetwork
        && !IPAddress.IsLoopback(a)
        && !a.ToString().StartsWith("169.254.", StringComparison.Ordinal);

    public void Dispose() => Stop();
}
