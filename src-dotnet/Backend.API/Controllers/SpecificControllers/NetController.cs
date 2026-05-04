using System.Text.Json;
using DistributedLocalSystem.Core.Infrastructure.Attributes;
using DistributedLocalSystem.Core.NetDiscovery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/net")]
[NotRedirect]
public class NetController : ControllerBase
{
    private readonly NetDiscoveryService _netService;
    private readonly IOptions<JsonOptions> _jsonOptions;

    public NetController(NetDiscoveryService netService, IOptions<JsonOptions> jsonOptions)
    {
        _netService = netService;
        _jsonOptions = jsonOptions;
    }

    [HttpGet("status")]
    public async Task GetStatus(CancellationToken cancellationToken)
    {
        var status = _netService.GetStatus();
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
        var status = _netService.GetStatus();
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            Response.Body,
            new { role = status.ConfiguredRole },
            _jsonOptions.Value.JsonSerializerOptions,
            cancellationToken
        );
    }
}
