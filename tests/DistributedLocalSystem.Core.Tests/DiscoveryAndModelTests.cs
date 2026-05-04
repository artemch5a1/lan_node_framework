using System.Net;
using System.Reflection;
using DistributedLocalSystem.Core.Discovery;
using DistributedLocalSystem.Core.Persistence;
using DistributedLocalSystem.Core.Persistence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Core.Tests;

public class DiscoveryAndModelTests
{
    [Fact]
    public void DiscoveryServiceIdentity_MatchesOnlySameAdvertisedName()
    {
        DiscoveryServiceIdentity id = DiscoveryServiceIdentity.FromConfiguredAppId("my-app");

        Assert.True(id.MatchesPeerAdvertisedName("my-app"));
        Assert.False(id.MatchesPeerAdvertisedName("other-app"));
        Assert.False(id.MatchesPeerAdvertisedName("MY-APP"));
        Assert.False(id.MatchesPeerAdvertisedName(null));
    }

    [Fact]
    public void DiscoveryServiceIdentity_FromConfiguredAppId_TrimsWhitespace()
    {
        DiscoveryServiceIdentity id = DiscoveryServiceIdentity.FromConfiguredAppId("  x  ");

        Assert.Equal("x", id.ExpectedServiceName);
        Assert.True(id.MatchesPeerAdvertisedName("x"));
    }

    [Fact]
    public void ParseRole_ReturnsHost_ForHostValue()
    {
        NetConfiguredRole role = DiscoveryOptions.ParseRole("host");

        Assert.Equal(NetConfiguredRole.Host, role);
    }

    [Fact]
    public void ParseRole_ReturnsClient_ForClientValue()
    {
        NetConfiguredRole role = DiscoveryOptions.ParseRole("client");

        Assert.Equal(NetConfiguredRole.Client, role);
    }

    [Fact]
    public void ParseRole_ReturnsNone_ForUnknownValue()
    {
        NetConfiguredRole role = DiscoveryOptions.ParseRole("unexpected");

        Assert.Equal(NetConfiguredRole.None, role);
    }

    [Fact]
    public void ParsedRole_IgnoresCaseAndWhitespace()
    {
        DiscoveryOptions options = new() { Role = " HOST " };

        Assert.Equal(NetConfiguredRole.Host, options.ParsedRole);
    }

    [Fact]
    public void NetRoleApi_Format_ReturnsExpectedString()
    {
        string formatted = NetRoleApi.Format(NetConfiguredRole.Client);

        Assert.Equal("client", formatted);
    }

    [Fact]
    public void DiscoveredServer_IsActive_ReturnsTrue_WithinExpirationWindow()
    {
        DiscoveredServer server = new(
            "op1-26",
            IPAddress.Parse("192.168.1.10"),
            DateTime.UtcNow.AddSeconds(-1)
        );

        Assert.True(server.IsActive(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void DiscoveredServer_IsActive_ReturnsFalse_WhenExpired()
    {
        DiscoveredServer server = new(
            "op1-26",
            IPAddress.Parse("192.168.1.10"),
            DateTime.UtcNow.AddSeconds(-10)
        );

        Assert.False(server.IsActive(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void DiscoveredServer_Equality_IgnoresLastSeen()
    {
        DiscoveredServer first = new(
            "op1-26",
            IPAddress.Parse("192.168.1.10"),
            DateTime.UtcNow.AddSeconds(-1)
        );
        DiscoveredServer second = new(
            "op1-26",
            IPAddress.Parse("192.168.1.10"),
            DateTime.UtcNow.AddMinutes(-5)
        );

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void NetDiscoveryService_GetStatus_ReturnsInitialSnapshotFromOptions()
    {
        NetDiscoveryService service = CreateService(
            new DiscoveryOptions
            {
                Role = "client",
                AppId = "demo-app",
                LanPort = 17891,
                UdpPort = 49152,
            }
        );

        NetStatusDto status = service.GetStatus();

        Assert.Equal("client", status.ConfiguredRole);
        Assert.Equal(NetDiscoveryState.Idle, status.State);
        Assert.Equal("demo-app", status.AppId);
        Assert.Equal(17891, status.LanPort);
        Assert.Equal(49152, status.UdpPort);
        Assert.Null(status.RemoteHostBaseUrl);
    }

    [Fact]
    public void NetDiscoveryService_TryGetHostProxyBaseUrl_ReturnsUrl_WhenClientIsConnected()
    {
        NetDiscoveryService service = CreateService();
        SetPrivateField(service, "_state", NetDiscoveryState.ClientConnected);
        SetPrivateField(service, "_remoteHostIp", "192.168.1.25");
        SetPrivateField(service, "_remoteTcpPort", 17891);

        bool success = service.TryGetHostProxyBaseUrl(out string? baseUrl);

        Assert.True(success);
        Assert.Equal("http://192.168.1.25:17891", baseUrl);
    }

    private static NetDiscoveryService CreateService(DiscoveryOptions? options = null)
    {
        DiscoveryOptions o = options ?? new DiscoveryOptions();
        StubNetDiscoverySettingsRepository stub = new(o);
        return new NetDiscoveryService(
            stub,
            DiscoveryServiceIdentity.FromRepository(stub),
            NullLogger<NetDiscoveryService>.Instance
        );
    }

    private sealed class StubNetDiscoverySettingsRepository : INetDiscoverySettingsRepository
    {
        private readonly DiscoveryOptions _o;

        public StubNetDiscoverySettingsRepository(DiscoveryOptions o) =>
            _o = NetDiscoverySettingsDefaults.Clone(o);

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public DiscoveryOptions GetCurrent() => NetDiscoverySettingsDefaults.Clone(_o);

        public Task<DiscoveryOptions> ReloadFromDatabaseAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(GetCurrent());
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo? field = target
            .GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
