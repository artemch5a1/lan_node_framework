using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers.SpecificControllers;

/// <summary>
/// Тело ответа <c>GET /api/net/role</c> (имя свойства сериализуется в camelCase).
/// </summary>
public sealed record NetRoleResponse(string Role);

public sealed record ConnectByIpRequest(string? IpAddress);

/// <summary>
/// API настройки LAN discovery: вызовы <see cref="INetLanOrchestrator"/> и перевод <see cref="Outcome{T}"/> в HTTP.
/// На границе домен ↔ транспорт (JSON-контракты как раньше).
/// </summary>
[ApiController]
[Route("api/net")]
[NotRedirect]
public sealed class NetController : ControllerBase
{
    private readonly INetLanOrchestrator _net;

    public NetController(INetLanOrchestrator net) => _net = net;

    [HttpGet("status")]
    [ProducesResponseType(typeof(NetStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<NetStatusDto> GetStatus()
    {
        Outcome<NetRuntimeSnapshot> outcome = _net.GetRuntimeSnapshot();
        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value.ToTransport();
    }

    [HttpGet("role")]
    [ProducesResponseType(typeof(NetRoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<NetRoleResponse> GetRole()
    {
        Outcome<string> outcome = _net.GetConfiguredRoleLabel();
        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return new NetRoleResponse(outcome.Value);
    }

    [HttpGet("lan-peers")]
    [ProducesResponseType(typeof(IReadOnlyList<LanPeerSnapshot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<LanPeerSnapshot>>> GetLanPeers(
        CancellationToken cancellationToken
    )
    {
        Outcome<IReadOnlyList<LanNodeDescriptor>> outcome = await _net.ListLanNodesAsync(
                cancellationToken
            )
            .ConfigureAwait(false);

        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value.Select(d => d.ToTransport()).ToList();
    }

    [HttpGet("configuration")]
    [ProducesResponseType(typeof(DiscoveryOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<DiscoveryOptions> GetConfiguration()
    {
        Outcome<NetConfigurationState> outcome = _net.GetConfigurationState();
        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value.ToTransport();
    }

    [HttpPut("configuration")]
    [ProducesResponseType(typeof(DiscoveryOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiscoveryOptions>> ChangeConfiguration(
        [FromBody] DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken
    )
    {
        NetConfigurationState next = NetConfigurationState.FromTransport(newDiscoveryOptions);
        Outcome<NetConfigurationState> outcome = await _net.ApplyConfigurationStateAsync(
                next,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value.ToTransport();
    }

    [HttpPost("disconnect")]
    [ProducesResponseType(typeof(DiscoveryOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiscoveryOptions>> DisconnectFromRemoteHost(
        CancellationToken cancellationToken
    )
    {
        Outcome<NetConfigurationState> outcome = await _net.DisconnectFromAssignedRemoteAsync(
                cancellationToken
            )
            .ConfigureAwait(false);

        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value.ToTransport();
    }

    [HttpPost("connect-by-ip")]
    [ProducesResponseType(typeof(ConnectByIpResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConnectByIpResult>> ConnectByIp(
        [FromBody] ConnectByIpRequest body,
        CancellationToken cancellationToken
    )
    {
        Outcome<ConnectByIpResult> outcome = await _net.ConnectToRemoteHostByIpAsync(
                body.IpAddress ?? "",
                cancellationToken
            )
            .ConfigureAwait(false);

        if (outcome.IsFailure)
            return NetFlowError(outcome.Error);

        return outcome.Value;
    }

    private ObjectResult NetFlowError(NetFlowError error) =>
        StatusCode(
            StatusCodeFor(error),
            new { error = new { code = error.Code, message = error.Message } }
        );

    private static int StatusCodeFor(NetFlowError error) =>
        error.Code switch
        {
            NetFlowErrorCodes.HostCollision => StatusCodes.Status409Conflict,
            NetFlowErrorCodes.AnotherHostAlreadyPresent => StatusCodes.Status409Conflict,
            NetFlowErrorCodes.OperationCancelled => StatusCodes.Status400BadRequest,
            NetFlowErrorCodes.InvalidConfiguration => StatusCodes.Status400BadRequest,
            NetFlowErrorCodes.RemoteHostUnreachable => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
}
