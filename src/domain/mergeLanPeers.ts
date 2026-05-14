import type { LanPeerSnapshot, NetStatus } from "../api/types";

/** Пока нет ответа сканирования — показываем текущий remote, чтобы можно было отключиться. */
export function mergeLanPeersWithStickyConnection(
  peers: LanPeerSnapshot[],
  net: NetStatus | null,
): LanPeerSnapshot[] {
  if (!net || net.configuredRole !== "client" || !net.remoteHostIp) return peers;
  const ip = net.remoteHostIp.trim();
  if (!ip || peers.some((p) => p.ipAddress === ip)) return peers;
  return [
    {
      ipAddress: ip,
      beaconName: "—",
      productSlug: net.productSlug ?? "",
      instanceSlug: "(нет в эфире)",
      seenInDiscovery: false,
    },
    ...peers,
  ];
}
