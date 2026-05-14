using System.Net;
using System.Reflection;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;
using DistributedLocalSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Core.Tests;

public class DiscoveryAndModelTests
{
    [Fact]
    public void DiscoveryServiceIdentity_MatchesExactBeacon_LegacyAppId()
    {
        DiscoveryServiceIdentity id = DiscoveryServiceIdentity.FromConfiguredAppId("my-app");

        Assert.True(id.MatchesPeerExactBeacon("my-app"));
        Assert.False(id.MatchesPeerExactBeacon("other-app"));
        Assert.False(id.MatchesPeerExactBeacon("MY-APP"));
        Assert.False(id.MatchesPeerExactBeacon(null));

        Assert.True(id.MatchesPeerSameProduct("my-app"));
        Assert.False(id.MatchesPeerSameProduct("other-app"));
    }

    [Fact]
    public void DiscoveryServiceIdentity_SameProductSlug_ForDlsBeaconNames()
    {
        DiscoveryServiceIdentity id = DiscoveryServiceIdentity.FromConfiguredAppId(
            "DLSv1-myprod-instancea"
        );

        Assert.True(id.MatchesPeerSameProduct("DLSv1-myprod-instanceb"));
        Assert.False(id.MatchesPeerSameProduct("DLSv1-other-instanceb"));
        Assert.False(id.MatchesPeerSameProduct("legacy-one-string"));
    }

    [Fact]
    public void DiscoveryServiceIdentity_FromConfiguredAppId_TrimsWhitespace()
    {
        DiscoveryServiceIdentity id = DiscoveryServiceIdentity.FromConfiguredAppId("  x  ");

        Assert.Equal("x", id.ExpectedServiceName);
        Assert.True(id.MatchesPeerExactBeacon("x"));
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
                ProductSlug = "demo",
                InstanceSlug = "app",
                LanPort = 17891,
                UdpPort = 49152,
            }
        );

        NetStatusDto status = service.GetStatus();

        Assert.Equal("client", status.ConfiguredRole);
        Assert.Equal(NetDiscoveryState.Idle, status.State);
        Assert.Equal("DLSv1-demo-app", status.AppId);
        Assert.Equal("demo", status.ProductSlug);
        Assert.Equal("app", status.InstanceSlug);
        Assert.Equal("", status.InstanceGuid);
        Assert.Equal(17891, status.LanPort);
        Assert.Equal(49152, status.UdpPort);
        Assert.Null(status.RemoteHostBaseUrl);
        Assert.NotNull(status.LocalIpv4Endpoints);
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
        private DiscoveryOptions _o;

        public StubNetDiscoverySettingsRepository(DiscoveryOptions o) =>
            _o = NetDiscoverySettingsDefaults.Clone(o);

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public DiscoveryOptions GetCurrent() => NetDiscoverySettingsDefaults.Clone(_o);

        public Task<DiscoveryOptions> ReloadFromDatabaseAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult(GetCurrent());

        public Task<DiscoveryOptions> UpdateConfiguration(
            DiscoveryOptions newDiscoveryOptions,
            CancellationToken cancellationToken = default
        )
        {
            _o = NetDiscoverySettingsDefaults.Clone(newDiscoveryOptions);
            return Task.FromResult(GetCurrent());
        }
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
