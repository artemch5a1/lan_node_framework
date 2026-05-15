using DistributedLocalSystem.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;

namespace DistributedLocalSystem.Core.Tests;

public sealed class ClientHostProxyHopTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("0", 0)]
    [InlineData("-1", 0)]
    [InlineData("abc", 0)]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    public void ReadIncomingHop_ParsesHeader(string? raw, int expected)
    {
        HeaderDictionary headers = new();
        if (raw is not null)
            headers[ClientHostProxyHop.HeaderName] = raw;

        int hop = ClientHostProxyHop.ReadIncomingHop(headers);

        Assert.Equal(expected, hop);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    public void ShouldRejectProxy_UsesMaxIncomingHop(int incoming, int max, bool expected)
    {
        bool reject = ClientHostProxyHop.ShouldRejectProxy(incoming, max);

        Assert.Equal(expected, reject);
    }

    [Fact]
    public void NextOutgoingHop_Increments()
    {
        Assert.Equal(1, ClientHostProxyHop.NextOutgoingHop(0));
        Assert.Equal(2, ClientHostProxyHop.NextOutgoingHop(1));
    }
}
