using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence.Abstractions;

namespace DistributedLocalSystem.Core.Udp;

public class ApiUdpAnnouncer : IDisposable
{
    private readonly UdpDiscovery.Net.UdpDiscovery _udpBroadcaster;
    private CancellationTokenSource? _broadcastCts;
    private Task? _broadcastTask;

    public ApiUdpAnnouncer(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity localIdentity
    )
    {
        DiscoveryOptions opt = settings.GetCurrent();
        _udpBroadcaster = new UdpDiscovery.Net.UdpDiscovery(
            serviceName: localIdentity.ExpectedServiceName,
            discoveryPort: (ushort)opt.UdpPort
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_broadcastTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        _broadcastCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _broadcastTask = Task.Run(
            () => _udpBroadcaster.StartBroadcasting(_broadcastCts.Token),
            _broadcastCts.Token
        );
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_broadcastCts is not null)
        {
            try
            {
                _broadcastCts.Cancel();
            }
            catch (ObjectDisposedException) { }

            if (_broadcastTask is not null)
            {
                try
                {
                    await _broadcastTask.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) { }
            }

            _broadcastCts.Dispose();
            _broadcastCts = null;
        }
    }

    public void Dispose()
    {
        _broadcastCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
