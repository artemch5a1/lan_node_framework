using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Udp;
using Microsoft.Extensions.Logging;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Краткий UDP-probe: есть ли уже другой хост с тем же beacon.</summary>
internal sealed class LanHostCollisionPreflight
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _localIdentity;
    private readonly ILogger _log;

    public LanHostCollisionPreflight(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity,
        ILogger log
    )
    {
        _settings = settings;
        _localIdentity = localIdentity;
        _log = log;
    }

    public bool AnotherHostAlreadyPresent(DiscoveryOptions opt)
    {
        using UdpDiscoveryService probe = new(
            _settings,
            _localIdentity,
            LanUdpPeerFilterKind.ExactBeaconName
        );
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
}
