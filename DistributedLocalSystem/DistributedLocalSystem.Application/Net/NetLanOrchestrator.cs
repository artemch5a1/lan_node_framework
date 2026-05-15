using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
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
    private readonly IHttpClientFactory _httpFactory;

    public NetLanOrchestrator(
        INetDiscoveryRuntime net,
        ILanPeerScanService lanPeerScan,
        INetDiscoveryConfigurationReloadCoordinator reloadCoordinator,
        IHttpClientFactory httpFactory
    )
    {
        _net = net;
        _lanPeerScan = lanPeerScan;
        _reloadCoordinator = reloadCoordinator;
        _httpFactory = httpFactory;
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
            NetConfiguredRole role = NetConfiguredRoleExtensions.ParseApiString(
                _net.GetStatus().ConfiguredRole
            );
            return Outcome<string>.Ok(role.GetDescription());
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

        Outcome<bool>? remoteValidateOutcome = await TryValidateRemoteHostBeforeClientConnectAsync(
                transport,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (remoteValidateOutcome is { IsFailure: true } failedRemote)
            return Outcome<NetConfigurationState>.Fail(failedRemote.Error);

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

    public async Task<Outcome<ConnectByIpResult>> ConnectToRemoteHostByIpAsync(
        string ipAddress,
        CancellationToken cancellationToken
    )
    {
        string trimmed = (ipAddress ?? "").Trim();
        if (
            string.IsNullOrEmpty(trimmed)
            || !IPAddress.TryParse(trimmed, out IPAddress? parsedAddr)
            || parsedAddr is null
        )
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Введите корректный IP-адрес (IPv4 или IPv6)."
            );
        }

        Outcome<NetRuntimeSnapshot> snapOutcome = GetRuntimeSnapshot();
        if (snapOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(snapOutcome.Error);

        NetRuntimeSnapshot snap = snapOutcome.Value;
        string canonicalIp = parsedAddr.ToString();
        if (IsConnectByIpTargetLocalMachine(parsedAddr, snap))
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Нельзя подключиться к адресу этого компьютера. Укажите IP другого узла в сети."
            );
        }

        DiscoveryOptions current = _net.GetCurrentConfiguration();
        if (
            !NetDiscoveryInputValidation.TryValidateLanPeerScanProduct(
                current.ProductSlug.Trim(),
                out NetFlowError? productError
            )
        )
            return Outcome<ConnectByIpResult>.Fail(productError!);

        int port = current.LanPort;
        HttpClient http = _httpFactory.CreateClient("NetRemoteProbe");
        Outcome<DiscoveryOptions> remoteOutcome = await NetRemoteConfigurationProbe.FetchAsync(
                http,
                parsedAddr,
                port,
                canonicalIp,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (remoteOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(remoteOutcome.Error);

        DiscoveryOptions remoteCfg = remoteOutcome.Value;

        Outcome<bool> remoteRoleOutcome = ValidateRemoteConnectTarget(remoteCfg);
        if (remoteRoleOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(remoteRoleOutcome.Error);

        string localProduct = current.ProductSlug.Trim();
        string remoteProduct = (remoteCfg.ProductSlug ?? "").Trim();
        if (!string.Equals(localProduct, remoteProduct, StringComparison.OrdinalIgnoreCase))
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Product slug на удалённом узле не совпадает с вашим. Это другая линейка продукта."
            );
        }

        string localGuid = current.InstanceGuid?.Trim() ?? "";
        string remoteGuid = remoteCfg.InstanceGuid?.Trim() ?? "";
        if (string.IsNullOrEmpty(remoteGuid))
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                "Ответ узла не содержит InstanceGuid — похоже, это не API этой программы."
            );
        }

        if (
            localGuid.Length > 0
            && string.Equals(localGuid, remoteGuid, StringComparison.OrdinalIgnoreCase)
        )
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Ответ с указанного адреса относится к этому же экземпляру (совпадает InstanceGuid). Укажите другой компьютер."
            );
        }

        DiscoveryOptions next = current.Clone();
        next.RemoteHostIp = canonicalIp;
        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(next);

        Outcome<NetConfigurationState> applyOutcome = await ApplyConfigurationStateAsync(
                NetConfigurationState.FromTransport(next),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (applyOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(applyOutcome.Error);

        NetConfigurationState saved = applyOutcome.Value;
        string remoteSlug = remoteCfg.InstanceSlug?.Trim() ?? "";
        string instanceLabel = LanBeaconName.IsValidSlug(remoteSlug)
            ? remoteSlug
            : "удалённый узел";

        LanPeerSnapshot peer = new(
            canonicalIp,
            remoteProduct,
            instanceLabel,
            SeenInDiscovery: false
        );

        ConnectByIpResult result = new(saved.ToTransport(), peer);
        return Outcome<ConnectByIpResult>.Ok(result);
    }

    public async Task<Outcome<NetConfigurationState>> DisconnectFromAssignedRemoteAsync(
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions current = _net.GetCurrentConfiguration();
        DiscoveryOptions next = current.Clone();
        next.RemoteHostIp = null;
        next.Role = NetConfiguredRole.Host.ToApiString();

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

    /// <summary>
    /// При переходе в client проверяет удалённый узел по HTTP.
    /// <c>null</c> — проверка не требуется (режим host).
    /// </summary>
    private async Task<Outcome<bool>?> TryValidateRemoteHostBeforeClientConnectAsync(
        DiscoveryOptions transport,
        CancellationToken cancellationToken
    )
    {
        if (!transport.ParsedRole.IsClientRole())
            return null;

        string? remoteIp = transport.RemoteHostIp?.Trim();
        if (string.IsNullOrEmpty(remoteIp) || !IPAddress.TryParse(remoteIp, out IPAddress? remoteAddr))
        {
            return Outcome<bool>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Укажите корректный IP удалённого хоста."
            );
        }

        DiscoveryOptions current = _net.GetCurrentConfiguration();
        HttpClient http = _httpFactory.CreateClient("NetRemoteProbe");
        Outcome<DiscoveryOptions> remoteOutcome = await NetRemoteConfigurationProbe.FetchAsync(
                http,
                remoteAddr,
                current.LanPort,
                remoteAddr.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);
        if (remoteOutcome.IsFailure)
            return Outcome<bool>.Fail(remoteOutcome.Error);

        return ValidateRemoteConnectTarget(remoteOutcome.Value);
    }

    private static Outcome<bool> ValidateRemoteConnectTarget(DiscoveryOptions remoteConfiguration)
    {
        if (
            NetRemoteConnectionValidation.TryValidateRemoteConnectTarget(
                remoteConfiguration,
                out string? userMessage
            )
        )
            return Outcome<bool>.Ok(true);

        return Outcome<bool>.Fail(
            NetFlowErrorCodes.RemoteHostIsClient,
            userMessage ?? NetRemoteConnectionValidation.RemoteIsClientMessage
        );
    }

    private static bool IsConnectByIpTargetLocalMachine(IPAddress target, NetRuntimeSnapshot snap)
    {
        if (IPAddress.IsLoopback(target))
            return true;

        if (!string.IsNullOrWhiteSpace(snap.ThisHostIp))
        {
            if (
                IPAddress.TryParse(snap.ThisHostIp.Trim(), out IPAddress? reported)
                && reported is not null
                && IpAddressesEqualNormalized(target, reported)
            )
                return true;
        }

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation u in ni.GetIPProperties().UnicastAddresses)
            {
                if (
                    u.Address.AddressFamily
                    is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                )
                    continue;

                if (IpAddressesEqualNormalized(target, u.Address))
                    return true;
            }
        }

        return false;
    }

    private static bool IpAddressesEqualNormalized(IPAddress a, IPAddress b)
    {
        if (a.Equals(b))
            return true;

        try
        {
            IPAddress na =
                a.AddressFamily == AddressFamily.InterNetworkV6 && a.IsIPv4MappedToIPv6
                    ? a.MapToIPv4()
                    : a;
            IPAddress nb =
                b.AddressFamily == AddressFamily.InterNetworkV6 && b.IsIPv4MappedToIPv6
                    ? b.MapToIPv4()
                    : b;
            return na.Equals(nb);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAnotherHostPresent(string message) =>
        message.Contains("another host", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Net: another host", StringComparison.OrdinalIgnoreCase);
}
