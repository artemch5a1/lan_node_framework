namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Ответ <c>POST /api/net/connect-by-ip</c>: сохранённая конфигурация и подпись узла для списка.</summary>
public sealed record ConnectByIpResult(DiscoveryOptions Configuration, LanPeerSnapshot Peer);
