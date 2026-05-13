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
        catch (Exception)
        {
            return Outcome<NetRuntimeSnapshot>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }

    public Outcome<string> GetConfiguredRoleLabel()
    {
        try
        {
            return Outcome<string>.Ok(_net.GetStatus().ConfiguredRole);
        }
        catch (Exception)
        {
            return Outcome<string>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }

    public async Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ListLanNodesAsync(
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions cur = _net.GetCurrentConfiguration();
        string product = cur.ProductSlug.Trim();
        if (
            !NetDiscoveryInputValidation.TryValidateLanPeerScanProduct(
                product,
                out NetFlowError? productError
            )
        )
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(productError!);
        }

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
        catch (Exception)
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(
                NetFlowErrorCodes.LanPeerScan,
                NetApiUserMessages.LanPeerScanFailed
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
        catch (Exception)
        {
            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }

    public async Task<Outcome<NetConfigurationState>> ApplyConfigurationStateAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions transport = next.ToTransport();
        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(transport);

        if (!NetDiscoveryInputValidation.TryValidatePersist(transport, out NetFlowError? ve))
            return Outcome<NetConfigurationState>.Fail(ve!);

        try
        {
            DiscoveryOptions updated = await _net.ChangeConfiguration(transport, cancellationToken)
                .ConfigureAwait(false);
            await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
            return Outcome<NetConfigurationState>.Ok(NetConfigurationState.FromTransport(updated));
        }
        catch (InvalidOperationException ex)
        {
            if (IsAnotherHostPresent(ex.Message))
                return Outcome<NetConfigurationState>.Fail(
                    new AnotherHostAlreadyPresentFault().ToFlowError()
                );

            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.HostCollision,
                NetApiUserMessages.HostRoleConflict
            );
        }
        catch (Exception)
        {
            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.ConfigurationUpdate,
                NetApiUserMessages.ConfigurationSaveFailed
            );
        }
    }

    public async Task<Outcome<NetConfigurationState>> DisconnectFromAssignedRemoteAsync(
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions current = _net.GetCurrentConfiguration();
        DiscoveryOptions next = current.Clone();
        next.RemoteHostIp = null;
        next.Role = "host";

        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(next);

        if (!NetDiscoveryInputValidation.TryValidatePersist(next, out NetFlowError? ve))
            return Outcome<NetConfigurationState>.Fail(ve!);

        try
        {
            DiscoveryOptions updated = await _net.ChangeConfiguration(next, cancellationToken)
                .ConfigureAwait(false);
            await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
            return Outcome<NetConfigurationState>.Ok(NetConfigurationState.FromTransport(updated));
        }
        catch (InvalidOperationException ex)
        {
            if (IsAnotherHostPresent(ex.Message))
                return Outcome<NetConfigurationState>.Fail(
                    new AnotherHostAlreadyPresentFault().ToFlowError()
                );

            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.HostCollision,
                NetApiUserMessages.HostRoleConflict
            );
        }
        catch (Exception)
        {
            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.ConfigurationReload,
                NetApiUserMessages.ConfigurationReloadAfterDisconnectFailed
            );
        }
    }

    private static bool IsAnotherHostPresent(string message) =>
        message.Contains("another host", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Net: another host", StringComparison.OrdinalIgnoreCase);
}
