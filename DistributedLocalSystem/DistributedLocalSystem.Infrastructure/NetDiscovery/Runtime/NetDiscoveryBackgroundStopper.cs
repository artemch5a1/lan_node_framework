using DistributedLocalSystem.Infrastructure.Udp;
using Microsoft.Extensions.Logging;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Остановка фоновых задач discovery (announcer, UDP client, CTS).</summary>
internal sealed class NetDiscoveryBackgroundStopper
{
    private readonly ILogger _log;

    public NetDiscoveryBackgroundStopper(ILogger log) => _log = log;

    public void Stop(
        ref CancellationTokenSource? runCts,
        ref Task? runTask,
        ref ApiUdpAnnouncer? hostAnnouncer,
        ref UdpDiscoveryService? clientDiscovery
    )
    {
        using CancellationTokenSource stopCts = new(TimeSpan.FromSeconds(2));
        CancellationToken stopToken = stopCts.Token;

        try
        {
            runCts?.Cancel();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Net: Cancel on shutdown");
        }

        try
        {
            runTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Net: background task wait on shutdown");
        }

        try
        {
            hostAnnouncer?.StopAsync(stopToken).GetAwaiter().GetResult();
        }
        catch { }
        finally
        {
            hostAnnouncer?.Dispose();
            hostAnnouncer = null;
        }

        try
        {
            clientDiscovery?.StopAsync(stopToken).GetAwaiter().GetResult();
        }
        catch { }
        finally
        {
            clientDiscovery?.Dispose();
            clientDiscovery = null;
        }

        runCts?.Dispose();
        runCts = null;
        runTask = null;
    }
}
