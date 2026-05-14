using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonProbeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

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
        if (
            !string.IsNullOrWhiteSpace(snap.ThisHostIp)
            && IPAddress.TryParse(snap.ThisHostIp.Trim(), out IPAddress? selfAddr)
            && selfAddr is not null
            && selfAddr.Equals(parsedAddr)
        )
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Нельзя подключиться к собственному IP этого компьютера."
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
        Uri configUri = BuildRemoteConfigurationUri(parsedAddr, port);

        HttpClient http = _httpFactory.CreateClient("NetRemoteProbe");
        DiscoveryOptions? remoteCfg;
        try
        {
            using HttpResponseMessage response = await http
                .GetAsync(configUri, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Outcome<ConnectByIpResult>.Fail(
                    NetFlowErrorCodes.RemoteHostUnreachable,
                    $"Сервер по адресу {canonicalIp} недоступен (HTTP {(int)response.StatusCode}). "
                        + "Проверьте IP и что на том компьютере запущена та же программа."
                );
            }

            remoteCfg = await response.Content
                .ReadFromJsonAsync<DiscoveryOptions>(JsonProbeOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.OperationCancelled,
                "Операция отменена."
            );
        }
        catch (HttpRequestException)
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                $"Не удалось связаться с {canonicalIp}:{port}. "
                    + "Убедитесь, что узел в сети и порт LAN совпадает с вашим."
            );
        }
        catch (JsonException)
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                "Удалённый узел ответил, но конфигурация не распознана. Версия приложения может отличаться."
            );
        }
        catch (Exception)
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }

        if (remoteCfg is null)
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                "Пустой ответ конфигурации с удалённого узла."
            );
        }

        string localProduct = current.ProductSlug.Trim();
        string remoteProduct = (remoteCfg.ProductSlug ?? "").Trim();
        if (!string.Equals(localProduct, remoteProduct, StringComparison.OrdinalIgnoreCase))
        {
            return Outcome<ConnectByIpResult>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Product slug на удалённом узле не совпадает с вашим. Это другая линейка продукта."
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
        string instanceLabel = LanBeaconName.IsValidSlug(remoteSlug) ? remoteSlug : "удалённый узел";

        LanPeerSnapshot peer = new(canonicalIp, remoteProduct, instanceLabel, SeenInDiscovery: false);

        ConnectByIpResult result = new(saved.ToTransport(), peer);
        return Outcome<ConnectByIpResult>.Ok(result);
    }

    private static Uri BuildRemoteConfigurationUri(IPAddress addr, int lanPort)
    {
        string host =
            addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                ? $"[{addr}]"
                : addr.ToString();
        return new Uri($"http://{host}:{lanPort}/api/net/configuration");
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
