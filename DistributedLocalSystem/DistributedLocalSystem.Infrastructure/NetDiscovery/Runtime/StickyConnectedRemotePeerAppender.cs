using System.Net;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>
/// Добавляет в список текущий remote, даже если beacon пропал (режим client).
/// </summary>
internal static class StickyConnectedRemotePeerAppender
{
    public static void AppendIfMissing(List<LanPeerSnapshot> list, DiscoveryOptions configuration)
    {
        if (configuration.ParsedRole != NetConfiguredRole.Client)
            return;

        string? raw = configuration.RemoteHostIp?.Trim();
        if (string.IsNullOrEmpty(raw) || !IPAddress.TryParse(raw, out _))
            return;

        if (list.Exists(p => string.Equals(p.IpAddress, raw, StringComparison.Ordinal)))
            return;

        list.Insert(
            0,
            new LanPeerSnapshot(
                raw,
                configuration.ProductSlug.Trim(),
                "(нет в эфире)",
                SeenInDiscovery: false
            )
        );
    }
}
