using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Изменяемое runtime-состояние discovery (под <c>lock</c> сервиса).</summary>
internal sealed class NetDiscoveryLiveState
{
    public NetDiscoveryState State { get; set; } = NetDiscoveryState.Idle;
    public string? ThisHostIp { get; set; }
    public NetDiscoveryPeerBinding Peer { get; } = new();
}
