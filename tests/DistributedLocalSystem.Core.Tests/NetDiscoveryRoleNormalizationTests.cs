using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Persistence;

namespace DistributedLocalSystem.Core.Tests;

public class NetDiscoveryRoleNormalizationTests
{
    [Fact]
    public void ApplyRoleFromRemoteHost_SetsClient_WhenValidIp()
    {
        DiscoveryOptions o = new() { Role = "host", RemoteHostIp = "192.168.1.5" };
        NetDiscoverySettingsDefaults.ApplyRoleFromRemoteHost(o);
        Assert.Equal("client", o.Role);
        Assert.Equal("192.168.1.5", o.RemoteHostIp);
    }

    [Fact]
    public void ApplyRoleFromRemoteHost_SetsHost_WhenRemoteEmpty()
    {
        DiscoveryOptions o = new() { Role = "client", RemoteHostIp = null };
        NetDiscoverySettingsDefaults.ApplyRoleFromRemoteHost(o);
        Assert.Equal("host", o.Role);
        Assert.Null(o.RemoteHostIp);
    }

    [Fact]
    public void ApplyRoleFromRemoteHost_ClearsInvalidIp()
    {
        DiscoveryOptions o = new() { Role = "client", RemoteHostIp = "not-an-ip" };
        NetDiscoverySettingsDefaults.ApplyRoleFromRemoteHost(o);
        Assert.Equal("host", o.Role);
        Assert.Null(o.RemoteHostIp);
    }
}
