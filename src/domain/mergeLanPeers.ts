import type { LanPeerSnapshot, NetStatus } from "../api/types";

/** Пока нет ответа сканирования — показываем текущий remote, чтобы можно было отключиться. */
export function mergeLanPeersWithStickyConnection(
  peers: LanPeerSnapshot[],
  net: NetStatus | null,
  manualOverlay: LanPeerSnapshot | null = null,
): LanPeerSnapshot[] {
  let merged: LanPeerSnapshot[];
  if (!net || net.configuredRole !== "client" || !net.remoteHostIp?.trim()) {
    merged = peers;
  } else {
    const ip = net.remoteHostIp.trim();
    if (!ip || peers.some((p) => p.ipAddress.trim() === ip)) {
      merged = peers;
    } else {
      merged = [
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
  }

  if (manualOverlay) {
    const mip = manualOverlay.ipAddress.trim();
    merged = merged.filter((p) => p.ipAddress.trim() !== mip);
    merged = [manualOverlay, ...merged];
  }

  return merged;
}
