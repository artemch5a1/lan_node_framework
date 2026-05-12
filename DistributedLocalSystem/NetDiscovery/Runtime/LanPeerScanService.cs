using System.Collections.Concurrent;
using System.Net;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.Udp;
using UdpDiscovery.Net;

namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>Сканирование LAN: узлы с тем же <see cref="DiscoveryOptions.ProductSlug"/> (формат DLSv1).</summary>
public sealed class LanPeerScanService : ILanPeerScanService
{
    private readonly INetDiscoverySettingsRepository _settings;
    private readonly DiscoveryServiceIdentity _identity;
    private readonly ILogger<LanPeerScanService> _log;

    public LanPeerScanService(
        INetDiscoverySettingsRepository settings,
        DiscoveryServiceIdentity identity,
        ILogger<LanPeerScanService> log
    )
    {
        _settings = settings;
        _identity = identity;
        _log = log;
    }

    public async Task<IReadOnlyList<LanPeerSnapshot>> ScanAsync(
        CancellationToken cancellationToken = default
    )
    {
        DiscoveryOptions opt = _settings.GetCurrent();
        List<LanPeerSnapshot> list = new();

        string product = opt.ProductSlug.Trim();
        if (!LanBeaconName.IsValidSlug(product))
        {
            _log.LogWarning("LanPeerScan: ProductSlug is not a valid slug; skipping UDP scan.");
        }
        else
        {
            ConcurrentDictionary<string, LanPeerSnapshot> map = new(StringComparer.Ordinal);

            void OnDiscovered(DiscoveredServer server)
            {
                if (!LanBeaconName.TryParse(server.Name, out LanBeaconParsed parsed))
                    return;
                if (!string.Equals(parsed.ProductSlug, product, StringComparison.Ordinal))
                    return;

                if (
                    string.Equals(
                        server.Name,
                        _identity.ExpectedServiceName,
                        StringComparison.Ordinal
                    )
                )
                    return;

                string ip = server.IpAddress.ToString();
                string key = $"{ip}\u001f{server.Name}";
                map[key] = new LanPeerSnapshot(
                    ip,
                    server.Name,
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
            catch (OperationCanceledException)
            {
                // normal when caller cancels
            }

            await udp.StopAsync(CancellationToken.None).ConfigureAwait(false);
            udp.ServerDiscovered -= OnDiscovered;

            list.AddRange(map.Values);
        }

        PrependStickyRemoteIfNeeded(list, opt);
        return list;
    }

    /// <summary>
    /// Текущее подключение client → remote: всегда показываем в списке, даже если beacon пропал.
    /// </summary>
    private static void PrependStickyRemoteIfNeeded(
        List<LanPeerSnapshot> list,
        DiscoveryOptions cur
    )
    {
        if (cur.ParsedRole != NetConfiguredRole.Client)
            return;

        string? raw = cur.RemoteHostIp?.Trim();
        if (string.IsNullOrEmpty(raw) || !IPAddress.TryParse(raw, out _))
            return;

        if (list.Exists(p => string.Equals(p.IpAddress, raw, StringComparison.Ordinal)))
            return;

        list.Insert(
            0,
            new LanPeerSnapshot(
                raw,
                "—",
                cur.ProductSlug.Trim(),
                "(нет в эфире)",
                SeenInDiscovery: false
            )
        );
    }
}
