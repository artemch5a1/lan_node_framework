using System.Collections.Concurrent;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;
using DistributedLocalSystem.Infrastructure.Udp;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Сканирование LAN: UDP-сбор пиров и дополнение sticky-remote.</summary>
public sealed class LanPeerScanService : ILanPeerScanService
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _identity;

    public LanPeerScanService(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity identity
    )
    {
        _settings = settings;
        _identity = identity;
    }

    public async Task<IReadOnlyList<LanPeerSnapshot>> ScanAsync(
        CancellationToken cancellationToken = default
    )
    {
        DiscoveryOptions opt = _settings.GetCurrent();
        IReadOnlyList<LanPeerSnapshot> discovered = await CollectFromUdpAsync(
                opt,
                cancellationToken
            )
            .ConfigureAwait(false);

        List<LanPeerSnapshot> list = discovered.ToList();
        StickyConnectedRemotePeerAppender.AppendIfMissing(list, opt);
        return list;
    }

    private async Task<IReadOnlyList<LanPeerSnapshot>> CollectFromUdpAsync(
        DiscoveryOptions opt,
        CancellationToken cancellationToken
    )
    {
        string product = opt.ProductSlug.Trim();
        ConcurrentDictionary<string, LanPeerSnapshot> map = new(StringComparer.Ordinal);

        void OnDiscovered(DiscoveredServer server)
        {
            if (!LanBeaconName.TryParse(server.Name, out LanBeaconParsed parsed))
                return;
            if (!string.Equals(parsed.ProductSlug, product, StringComparison.Ordinal))
                return;
            if (string.Equals(server.Name, _identity.ExpectedServiceName, StringComparison.Ordinal))
                return;

            string ip = server.IpAddress.ToString();
            string key = $"{ip}\u001f{server.Name}";
            map[key] = new LanPeerSnapshot(
                ip,
                parsed.ProductSlug,
                parsed.InstanceSlug,
                SeenInDiscovery: true
            );
        }

        using var udp = new UdpDiscoveryService(
            _settings,
            _identity,
            LanUdpPeerFilterKind.SameProductSlug
        );
        udp.ServerDiscovered += OnDiscovered;

        await udp.StartAsync(cancellationToken).ConfigureAwait(false);

        int ms = Math.Clamp(opt.DiscoveryTimeoutMs, 500, 10000);
        try
        {
            await Task.Delay(ms, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }

        await udp.StopAsync(CancellationToken.None).ConfigureAwait(false);
        udp.ServerDiscovered -= OnDiscovered;

        return map.Values.ToList();
    }
}
