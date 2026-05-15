using DistributedLocalSystem.Application.Net.Configuration;
using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Discovery;

/// <summary>Сканирование узлов LAN и маппинг в доменные дескрипторы.</summary>
public sealed class LanNodeListService
{
    private readonly INetDiscoveryRuntime _net;
    private readonly ILanPeerScanService _lanPeerScan;

    public LanNodeListService(INetDiscoveryRuntime net, ILanPeerScanService lanPeerScan)
    {
        _net = net;
        _lanPeerScan = lanPeerScan;
    }

    public async Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ListAsync(
        CancellationToken cancellationToken
    )
    {
        DiscoveryOptions cur = _net.GetCurrentConfiguration();
        string product = cur.ProductSlug.Trim();
        if (
            !NetDiscoveryInputValidation.TryValidateLanPeerScanProduct(
                product,
                out NetFlowError? productError
            )
        )
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(productError!);

        try
        {
            IReadOnlyList<LanPeerSnapshot> raw = await _lanPeerScan
                .ScanAsync(cancellationToken)
                .ConfigureAwait(false);
            LanNodeDescriptor[] mapped = raw.Select(LanNodeDescriptor.FromTransport).ToArray();
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Ok(mapped);
        }
        catch (OperationCanceledException)
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(
                NetFlowErrorCodes.OperationCancelled,
                "Операция отменена."
            );
        }
        catch (Exception)
        {
            return Outcome<IReadOnlyList<LanNodeDescriptor>>.Fail(
                NetFlowErrorCodes.LanPeerScan,
                NetApiUserMessages.LanPeerScanFailed
            );
        }
    }
}
