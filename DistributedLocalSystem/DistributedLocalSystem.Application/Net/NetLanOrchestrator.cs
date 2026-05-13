using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net;

/// <summary>
/// Оркестрация LAN-сценариев над инфраструктурными контрактами (маппинг транспорт ↔ домен).
/// </summary>
public sealed class NetLanOrchestrator : INetLanOrchestrator
{
    private readonly INetDiscoveryRuntime _net;
    private readonly ILanPeerScanService _lanPeerScan;
    private readonly INetDiscoveryConfigurationReloadCoordinator _reloadCoordinator;

    public NetLanOrchestrator(
        INetDiscoveryRuntime net,
        ILanPeerScanService lanPeerScan,
        INetDiscoveryConfigurationReloadCoordinator reloadCoordinator
    )
    {
        _net = net;
        _lanPeerScan = lanPeerScan;
        _reloadCoordinator = reloadCoordinator;
    }

    public Outcome<NetRuntimeSnapshot> GetRuntimeSnapshot()
    {
        try
        {
            return Outcome<NetRuntimeSnapshot>.Ok(
                NetRuntimeSnapshot.FromTransport(_net.GetStatus())
            );
        }
        catch (Exception ex)
        {
            return Outcome<NetRuntimeSnapshot>.FromException(NetFlowErrorCodes.Unexpected, ex);
        }
    }

    public Outcome<string> GetConfiguredRoleLabel()
    {
        try
        {
            return Outcome<string>.Ok(_net.GetStatus().ConfiguredRole);
        }
        catch (Exception ex)
        {
            return Outcome<string>.FromException(NetFlowErrorCodes.Unexpected, ex);
        }
    }

    public async Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ListLanNodesAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            IReadOnlyList<LanPeerSnapshot> raw = await _lanPeerScan
                .ScanAsync(cancellationToken)
                .ConfigureAwait(false);
            LanNodeDescriptor[] mapped = raw.Select(LanNodeDescriptor.FromTransport).ToArray();
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Ok(mapped);
        }
        catch (OperationCanceledException)
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(
                NetFlowErrorCodes.OperationCancelled,
                "Операция отменена."
            );
        }
        catch (Exception ex)
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.FromException(
                NetFlowErrorCodes.LanPeerScan,
                ex
            );
        }
    }

    public Outcome<NetConfigurationState> GetConfigurationState()
    {
        try
        {
            return Outcome<NetConfigurationState>.Ok(
                NetConfigurationState.FromTransport(_net.GetCurrentConfiguration())
            );
        }
        catch (Exception ex)
        {
            return Outcome<NetConfigurationState>.FromException(NetFlowErrorCodes.Unexpected, ex);
        }
    }

    public async Task<Outcome<NetConfigurationState>> ApplyConfigurationStateAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    )
    {
        try
        {
            DiscoveryOptions transport = next.ToTransport();
            DiscoveryOptions updated = await _net.ChangeConfiguration(transport, cancellationToken)
                .ConfigureAwait(false);
            await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
            return Outcome<NetConfigurationState>.Ok(NetConfigurationState.FromTransport(updated));
        }
        catch (InvalidOperationException ex)
        {
            if (IsAnotherHostPresent(ex.Message))
            {
                AnotherHostAlreadyPresentFault fault = new(ex.Message);
                return Outcome<NetConfigurationState>.Fail(fault.ToFlowError());
            }

            return Outcome<NetConfigurationState>.Fail(NetFlowErrorCodes.HostCollision, ex.Message);
        }
        catch (Exception ex)
        {
            return Outcome<NetConfigurationState>.FromException(
                NetFlowErrorCodes.ConfigurationUpdate,
                ex
            );
        }
    }

    public async Task<Outcome<NetConfigurationState>> DisconnectFromAssignedRemoteAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            DiscoveryOptions current = _net.GetCurrentConfiguration();
            DiscoveryOptions next = current.Clone();
            next.RemoteHostIp = null;
            next.Role = "host";

            DiscoveryOptions updated = await _net.ChangeConfiguration(next, cancellationToken)
                .ConfigureAwait(false);
            await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
            return Outcome<NetConfigurationState>.Ok(NetConfigurationState.FromTransport(updated));
        }
        catch (InvalidOperationException ex)
        {
            if (IsAnotherHostPresent(ex.Message))
            {
                AnotherHostAlreadyPresentFault fault = new(ex.Message);
                return Outcome<NetConfigurationState>.Fail(fault.ToFlowError());
            }

            return Outcome<NetConfigurationState>.Fail(NetFlowErrorCodes.HostCollision, ex.Message);
        }
        catch (Exception ex)
        {
            return Outcome<NetConfigurationState>.FromException(
                NetFlowErrorCodes.ConfigurationReload,
                ex
            );
        }
    }

    private static bool IsAnotherHostPresent(string message) =>
        message.Contains("another host", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Net: another host", StringComparison.OrdinalIgnoreCase);
}
