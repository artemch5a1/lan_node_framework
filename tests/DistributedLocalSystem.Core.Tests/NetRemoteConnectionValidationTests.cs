using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Tests;

public class NetRemoteConnectionValidationTests
{
    [Fact]
    public void TryValidateRemoteConnectTarget_Fails_WhenRemoteIsClient()
    {
        DiscoveryOptions remote = new() { Role = "client", RemoteHostIp = "192.168.1.50" };

        bool ok = NetRemoteConnectionValidation.TryValidateRemoteConnectTarget(
            remote,
            out string? message
        );

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateRemoteConnectTarget_Succeeds_WhenRemoteIsHost()
    {
        DiscoveryOptions remote = new() { Role = "host" };

        bool ok = NetRemoteConnectionValidation.TryValidateRemoteConnectTarget(
            remote,
            out string? message
        );

        Assert.True(ok);
        Assert.Null(message);
    }
}
