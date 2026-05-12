using System.Text.Json;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Infrastructure.Attributes;
using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/net")]
[NotRedirect]
public class NetController : ControllerBase
{
    private readonly NetDiscoveryService _netService;
    private readonly INetDiscoveryConfigurationReloadCoordinator _reloadCoordinator;
    private readonly ILanPeerScanService _lanPeerScan;
    private readonly IOptions<JsonOptions> _jsonOptions;

    public NetController(
        NetDiscoveryService netService,
        INetDiscoveryConfigurationReloadCoordinator reloadCoordinator,
        ILanPeerScanService lanPeerScan,
        IOptions<JsonOptions> jsonOptions
    )
    {
        _netService = netService;
        _reloadCoordinator = reloadCoordinator;
        _lanPeerScan = lanPeerScan;
        _jsonOptions = jsonOptions;
    }

    [HttpGet("status")]
    public async Task GetStatus(CancellationToken cancellationToken)
    {
        NetStatusDto status = _netService.GetStatus();
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            status,
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }

    [HttpGet("role")]
    public async Task GetRole(CancellationToken cancellationToken)
    {
        NetStatusDto status = _netService.GetStatus();
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            new { role = status.ConfiguredRole },
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }

    [HttpGet("lan-peers")]
    public async Task GetLanPeers(CancellationToken cancellationToken)
    {
        IReadOnlyList<LanPeerSnapshot> peers = await _lanPeerScan
            .ScanAsync(cancellationToken)
            .ConfigureAwait(false);
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            peers,
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }

    [HttpGet("configuration")]
    public async Task GetConfiguration(CancellationToken cancellationToken)
    {
        DiscoveryOptions configuration = _netService.GetCurrentConfiguration();
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            configuration,
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }

    [HttpPut("configuration")]
    public async Task ChangeConfiguration(
        [FromBody] DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions updated = await _netService.ChangeConfiguration(
            newDiscoveryOptions,
            cancellationToken
        );
        await _reloadCoordinator.ReloadAsync(cancellationToken);
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            updated,
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }

    /// <summary>Отключение от удалённого хоста: снова режим host (beacon), <c>RemoteHostIp</c> сбрасывается.</summary>
    [HttpPost("disconnect")]
    public async Task DisconnectFromRemoteHost(CancellationToken cancellationToken)
    {
        DiscoveryOptions current = _netService.GetCurrentConfiguration();
        DiscoveryOptions next = NetDiscoverySettingsDefaults.Clone(current);
        next.RemoteHostIp = null;
        next.Role = "host";

        DiscoveryOptions updated = await _netService.ChangeConfiguration(next, cancellationToken)
            .ConfigureAwait(false);
        await _reloadCoordinator.ReloadAsync(cancellationToken).ConfigureAwait(false);
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            updated,
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }
}
