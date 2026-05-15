using System.Net;
using System.Net.Http;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Remote;

/// <summary>HTTP-проверка удалённого хоста перед переходом в режим client.</summary>
public sealed class NetRemoteHostPreConnectValidator
{
    private readonly INetDiscoveryRuntime _net;
    private readonly IHttpClientFactory _httpFactory;

    public NetRemoteHostPreConnectValidator(
        INetDiscoveryRuntime net,
        IHttpClientFactory httpFactory
    )
    {
        _net = net;
        _httpFactory = httpFactory;
    }

    /// <summary><c>null</c> — проверка не требуется (режим host).</summary>
    public async Task<Outcome<bool>?> ValidateIfClientConnectAsync(
        DiscoveryOptions transport,
        CancellationToken cancellationToken
    )
    {
        if (!transport.ParsedRole.IsClientRole())
            return null;

        string? remoteIp = transport.RemoteHostIp?.Trim();
        if (
            string.IsNullOrEmpty(remoteIp)
            || !IPAddress.TryParse(remoteIp, out IPAddress? remoteAddr)
        )
        {
            return Outcome<bool>.Fail(
                NetFlowErrorCodes.InvalidConfiguration,
                "Укажите корректный IP удалённого хоста."
            );
        }

        DiscoveryOptions current = _net.GetCurrentConfiguration();
        HttpClient http = _httpFactory.CreateClient("NetRemoteProbe");
        Outcome<DiscoveryOptions> remoteOutcome = await NetRemoteConfigurationProbe
            .FetchAsync(http, remoteAddr, current.LanPort, remoteAddr.ToString(), cancellationToken)
            .ConfigureAwait(false);

        if (remoteOutcome.IsFailure)
            return Outcome<bool>.Fail(remoteOutcome.Error);

        return NetRemoteConnectTargetValidator.ValidateRole(remoteOutcome.Value);
    }
}
