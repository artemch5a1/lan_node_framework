using System.Net;
using System.Net.Http;
using DistributedLocalSystem.Application.Net.Configuration;
using DistributedLocalSystem.Application.Net.Remote;
using DistributedLocalSystem.Application.Net.Status;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Connection;

/// <summary>Сценарий подключения клиента к удалённому хосту по IP.</summary>
public sealed class NetConnectByIpService
{
    private readonly INetDiscoveryRuntime _net;
    private readonly IHttpClientFactory _httpFactory;
    private readonly NetRuntimeSnapshotReader _snapshotReader;
    private readonly NetConfigurationApplyService _configurationApply;
    private readonly ILocalMachineAddressMatcher _localAddressMatcher;

    public NetConnectByIpService(
        INetDiscoveryRuntime net,
        IHttpClientFactory httpFactory,
        NetRuntimeSnapshotReader snapshotReader,
        NetConfigurationApplyService configurationApply,
        ILocalMachineAddressMatcher localAddressMatcher
    )
    {
        _net = net;
        _httpFactory = httpFactory;
        _snapshotReader = snapshotReader;
        _configurationApply = configurationApply;
        _localAddressMatcher = localAddressMatcher;
    }

    public async Task<Outcome<ConnectByIpResult>> ConnectAsync(
        string ipAddress,
        CancellationToken cancellationToken
    )
    {
        Outcome<IPAddress> parseOutcome = ParseRemoteIp(ipAddress);
        if (parseOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(parseOutcome.Error);

        IPAddress parsedAddr = parseOutcome.Value;
        string canonicalIp = parsedAddr.ToString();

        Outcome<NetRuntimeSnapshot> snapOutcome = _snapshotReader.Read();
        if (snapOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(snapOutcome.Error);

        if (_localAddressMatcher.IsLocalMachine(parsedAddr, snapOutcome.Value.ThisHostIp))
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

        Outcome<DiscoveryOptions> remoteOutcome = await FetchRemoteConfigurationAsync(
                parsedAddr,
                current.LanPort,
                canonicalIp,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (remoteOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(remoteOutcome.Error);

        DiscoveryOptions remoteCfg = remoteOutcome.Value;

        Outcome<bool> validationOutcome = ValidateRemotePeer(current, remoteCfg);
        if (validationOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(validationOutcome.Error);

        DiscoveryOptions next = BuildClientConfiguration(current, canonicalIp);
        Outcome<NetConfigurationState> applyOutcome = await _configurationApply
            .ApplyAsync(next, cancellationToken)
            .ConfigureAwait(false);
        if (applyOutcome.IsFailure)
            return Outcome<ConnectByIpResult>.Fail(applyOutcome.Error);

        LanPeerSnapshot peer = NetRemoteConnectTargetValidator.BuildPeerSnapshot(
            canonicalIp,
            remoteCfg
        );
        return Outcome<ConnectByIpResult>.Ok(
            new ConnectByIpResult(applyOutcome.Value.ToTransport(), peer)
        );
    }

    private static Outcome<IPAddress> ParseRemoteIp(string ipAddress)
    {
        string trimmed = (ipAddress ?? "").Trim();
        if (
            string.IsNullOrEmpty(trimmed)
            || !IPAddress.TryParse(trimmed, out IPAddress? parsedAddr)
            || parsedAddr is null
        )
        {
            return Outcome<IPAddress>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Введите корректный IP-адрес (IPv4 или IPv6)."
            );
        }

        return Outcome<IPAddress>.Ok(parsedAddr);
    }

    private async Task<Outcome<DiscoveryOptions>> FetchRemoteConfigurationAsync(
        IPAddress parsedAddr,
        int lanPort,
        string canonicalIp,
        CancellationToken cancellationToken
    )
    {
        HttpClient http = _httpFactory.CreateClient("NetRemoteProbe");
        return await NetRemoteConfigurationProbe
            .FetchAsync(http, parsedAddr, lanPort, canonicalIp, cancellationToken)
            .ConfigureAwait(false);
    }

    private static Outcome<bool> ValidateRemotePeer(
        DiscoveryOptions localConfiguration,
        DiscoveryOptions remoteConfiguration
    )
    {
        Outcome<bool> roleOutcome = NetRemoteConnectTargetValidator.ValidateRole(
            remoteConfiguration
        );
        if (roleOutcome.IsFailure)
            return roleOutcome;

        Outcome<bool> productOutcome = NetRemoteConnectTargetValidator.ValidateProductSlug(
            localConfiguration,
            remoteConfiguration
        );
        if (productOutcome.IsFailure)
            return productOutcome;

        return NetRemoteConnectTargetValidator.ValidateInstanceGuid(
            localConfiguration,
            remoteConfiguration
        );
    }

    private static DiscoveryOptions BuildClientConfiguration(
        DiscoveryOptions current,
        string canonicalIp
    )
    {
        DiscoveryOptions next = current.Clone();
        next.RemoteHostIp = canonicalIp;
        NetDiscoveryConfigurationNormalizer.ApplyRoleFromRemoteHost(next);
        return next;
    }
}
