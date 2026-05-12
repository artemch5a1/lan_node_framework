using System.Net.Mime;
using System.Text.Json;
using DistributedLocalSystem.Application.Net.UseCases;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Backend.API.Controllers;

/// <summary>
/// API настройки LAN discovery: каждый маршрут вызывает отдельный use case и переводит <see cref="Outcome{T}"/> в HTTP.
/// На границе домен ↔ транспорт (JSON-контракты как раньше).
/// </summary>
[ApiController]
[Route("api/net")]
[NotRedirect]
public sealed class NetController : ControllerBase
{
    private readonly IGetNetStatusUseCase _getNetStatus;

    private readonly IGetNetRoleUseCase _getNetRole;

    private readonly IGetLanPeersUseCase _getLanPeers;

    private readonly IGetNetConfigurationUseCase _getNetConfiguration;

    private readonly IChangeNetConfigurationUseCase _changeNetConfiguration;

    private readonly IDisconnectFromRemoteHostUseCase _disconnectFromRemoteHost;

    private readonly IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> _jsonOptions;

    public NetController(
        IGetNetStatusUseCase getNetStatus,
        IGetNetRoleUseCase getNetRole,
        IGetLanPeersUseCase getLanPeers,
        IGetNetConfigurationUseCase getNetConfiguration,
        IChangeNetConfigurationUseCase changeNetConfiguration,
        IDisconnectFromRemoteHostUseCase disconnectFromRemoteHost,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions
    )
    {
        _getNetStatus = getNetStatus;

        _getNetRole = getNetRole;

        _getLanPeers = getLanPeers;

        _getNetConfiguration = getNetConfiguration;

        _changeNetConfiguration = changeNetConfiguration;

        _disconnectFromRemoteHost = disconnectFromRemoteHost;

        _jsonOptions = jsonOptions;
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task GetStatus(CancellationToken cancellationToken)
    {
        Outcome<NetRuntimeSnapshot> outcome = _getNetStatus.Execute();

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        await WriteOutcomeAsync(
                Outcome<NetStatusDto>.Ok(outcome.Value.ToTransport()),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    [HttpGet("role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task GetRole(CancellationToken cancellationToken)
    {
        Outcome<string> outcome = _getNetRole.Execute();

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;

        Response.ContentType = MediaTypeNames.Application.Json;

        await JsonSerializer
            .SerializeAsync(
                Response.Body,
                new { role = outcome.Value },
                _jsonOptions.Value.JsonSerializerOptions,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    [HttpGet("lan-peers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task GetLanPeers(CancellationToken cancellationToken)
    {
        Outcome<IReadOnlyList<LanNodeDescriptor>> outcome = await _getLanPeers
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        IReadOnlyList<LanPeerSnapshot> transport = outcome
            .Value.Select(d => d.ToTransport())
            .ToList();

        await WriteOutcomeAsync(
                Outcome<IReadOnlyList<LanPeerSnapshot>>.Ok(transport),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    [HttpGet("configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task GetConfiguration(CancellationToken cancellationToken)
    {
        Outcome<NetConfigurationState> outcome = _getNetConfiguration.Execute();

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        await WriteOutcomeAsync(
                Outcome<DiscoveryOptions>.Ok(outcome.Value.ToTransport()),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    [HttpPut("configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task ChangeConfiguration(
        [FromBody] DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken
    )
    {
        NetConfigurationState next = NetConfigurationState.FromTransport(newDiscoveryOptions);

        Outcome<NetConfigurationState> outcome = await _changeNetConfiguration
            .ExecuteAsync(next, cancellationToken)
            .ConfigureAwait(false);

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        await WriteOutcomeAsync(
                Outcome<DiscoveryOptions>.Ok(outcome.Value.ToTransport()),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    [HttpPost("disconnect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task DisconnectFromRemoteHost(CancellationToken cancellationToken)
    {
        Outcome<NetConfigurationState> outcome = await _disconnectFromRemoteHost
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        await WriteOutcomeAsync(
                Outcome<DiscoveryOptions>.Ok(outcome.Value.ToTransport()),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task WriteOutcomeAsync<T>(Outcome<T> outcome, CancellationToken cancellationToken)
    {
        if (outcome.IsFailure)
        {
            await WriteFailureAsync(outcome.Error, cancellationToken).ConfigureAwait(false);

            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;

        Response.ContentType = MediaTypeNames.Application.Json;

        await JsonSerializer
            .SerializeAsync(
                Response.Body,
                outcome.Value,
                _jsonOptions.Value.JsonSerializerOptions,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task WriteFailureAsync(NetFlowError error, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodeFor(error);

        Response.ContentType = MediaTypeNames.Application.Json;

        await JsonSerializer
            .SerializeAsync(
                Response.Body,
                new { error = new { code = error.Code, message = error.Message } },
                _jsonOptions.Value.JsonSerializerOptions,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private static int StatusCodeFor(NetFlowError error) =>
        error.Code switch
        {
            NetFlowErrorCodes.HostCollision => StatusCodes.Status409Conflict,

            NetFlowErrorCodes.AnotherHostAlreadyPresent => StatusCodes.Status409Conflict,

            NetFlowErrorCodes.OperationCancelled => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError,
        };
}
