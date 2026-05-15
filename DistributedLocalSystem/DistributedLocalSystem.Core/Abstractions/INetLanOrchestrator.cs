using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>
/// Единая точка оркестрации LAN (режимы, конфигурация, снимки, список узлов).
/// Скрывает детали реализации discovery (в т.ч. UDP broadcast); Application опирается только на этот контракт.
/// </summary>
public interface INetLanOrchestrator
{
    Outcome<NetRuntimeSnapshot> GetRuntimeSnapshot();

    Outcome<string> GetConfiguredRoleLabel();

    Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ListLanNodesAsync(
        CancellationToken cancellationToken
    );

    Outcome<NetConfigurationState> GetConfigurationState();

    Task<Outcome<NetConfigurationState>> ApplyConfigurationStateAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    );

    Task<Outcome<NetConfigurationState>> DisconnectFromAssignedRemoteAsync(
        CancellationToken cancellationToken
    );

    Task<Outcome<ConnectByIpResult>> ConnectToRemoteHostByIpAsync(
        string ipAddress,
        CancellationToken cancellationToken
    );
}
