using DistributedLocalSystem.Application.Net.Configuration;
using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Connection;

/// <summary>Отвязка от удалённого хоста и возврат в локальный режим host.</summary>
public sealed class NetDisconnectFromRemoteService
{
    private readonly INetDiscoveryRuntime _net;
    private readonly NetConfigurationPersistenceService _persistence;

    public NetDisconnectFromRemoteService(
        INetDiscoveryRuntime net,
        NetConfigurationPersistenceService persistence
    )
    {
        _net = net;
        _persistence = persistence;
    }

    public Task<Outcome<NetConfigurationState>> DisconnectAsync(CancellationToken cancellationToken)
    {
        DiscoveryOptions current = _net.GetCurrentConfiguration();
        DiscoveryOptions next = current.Clone();
        next.RemoteHostIp = null;
        next.Role = NetConfiguredRole.Host.ToApiString();
        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(next);

        return _persistence.SaveAsync(
            next,
            NetApiUserMessages.ConfigurationReloadAfterDisconnectFailed,
            cancellationToken
        );
    }
}
