using DistributedLocalSystem.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace DistributedLocalSystem.Core.Tests;

public class ClientHostProxyMiddlewareTests
{
    [Fact]
    public void ShouldBypass_ReturnsTrue_ForHealthEndpoint()
    {
        bool shouldBypass = ClientHostProxyMiddleware.ShouldBypass(new PathString("/health"));

        Assert.True(shouldBypass);
    }

    [Fact]
    public void ShouldBypass_ReturnsFalse_ForBusinessEndpoint()
    {
        bool shouldBypass = ClientHostProxyMiddleware.ShouldBypass(new PathString("/api/Books"));

        Assert.False(shouldBypass);
    }
}
