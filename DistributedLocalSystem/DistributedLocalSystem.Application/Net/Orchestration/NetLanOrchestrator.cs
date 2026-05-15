using DistributedLocalSystem.Application.Net.Configuration;
using DistributedLocalSystem.Application.Net.Connection;
using DistributedLocalSystem.Application.Net.Discovery;
using DistributedLocalSystem.Application.Net.Status;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Orchestration;

/// <summary>
/// Фасад LAN API: делегирует сценарии узким сервисам Application-слоя.
/// </summary>
public sealed class NetLanOrchestrator : INetLanOrchestrator
{
    private readonly NetRuntimeSnapshotReader _snapshotReader;
    private readonly NetConfigurationStateReader _configurationReader;
    private readonly LanNodeListService _lanNodeList;
    private readonly NetConfigurationApplyService _configurationApply;
    private readonly NetConnectByIpService _connectByIp;
    private readonly NetDisconnectFromRemoteService _disconnectFromRemote;

    public NetLanOrchestrator(
        NetRuntimeSnapshotReader snapshotReader,
        NetConfigurationStateReader configurationReader,
        LanNodeListService lanNodeList,
        NetConfigurationApplyService configurationApply,
        NetConnectByIpService connectByIp,
        NetDisconnectFromRemoteService disconnectFromRemote
    )
    {
        _snapshotReader = snapshotReader;
        _configurationReader = configurationReader;
        _lanNodeList = lanNodeList;
        _configurationApply = configurationApply;
        _connectByIp = connectByIp;
        _disconnectFromRemote = disconnectFromRemote;
    }

    public Outcome<NetRuntimeSnapshot> GetRuntimeSnapshot() => _snapshotReader.Read();

    public Outcome<string> GetConfiguredRoleLabel() => _snapshotReader.ReadConfiguredRoleLabel();

    public Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ListLanNodesAsync(
        CancellationToken cancellationToken
    ) => _lanNodeList.ListAsync(cancellationToken);

    public Outcome<NetConfigurationState> GetConfigurationState() => _configurationReader.Read();

    public Task<Outcome<NetConfigurationState>> ApplyConfigurationStateAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    ) => _configurationApply.ApplyAsync(next.ToTransport(), cancellationToken);

    public Task<Outcome<ConnectByIpResult>> ConnectToRemoteHostByIpAsync(
        string ipAddress,
        CancellationToken cancellationToken
    ) => _connectByIp.ConnectAsync(ipAddress, cancellationToken);

    public Task<Outcome<NetConfigurationState>> DisconnectFromAssignedRemoteAsync(
        CancellationToken cancellationToken
    ) => _disconnectFromRemote.DisconnectAsync(cancellationToken);
}
