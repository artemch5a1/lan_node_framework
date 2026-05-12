using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>Сканирование LAN по UDP для списка узлов с тем же product slug.</summary>
public interface ILanPeerScanService
{
    Task<IReadOnlyList<LanPeerSnapshot>> ScanAsync(CancellationToken cancellationToken = default);
}
