import type {
  ConnectByIpResult,
  DiscoveryOptions,
  LanPeerSnapshot,
  NetRoleResponse,
  NetStatus,
} from "./types";
import { parseNetErrorResponse } from "../services/notificationFormatting";

async function assertOk(response: Response): Promise<void> {
  if (response.ok) return;
  throw new Error(await parseNetErrorResponse(response));
}

export async function fetchNetRole(baseUrl: string): Promise<NetRoleResponse> {
  const r = await fetch(`${baseUrl}/api/net/role`);
  await assertOk(r);
  return (await r.json()) as NetRoleResponse;
}

export async function fetchNetStatus(baseUrl: string): Promise<NetStatus> {
  const r = await fetch(`${baseUrl}/api/net/status`);
  await assertOk(r);
  return (await r.json()) as NetStatus;
}

export async function fetchNetConfiguration(baseUrl: string): Promise<DiscoveryOptions> {
  const r = await fetch(`${baseUrl}/api/net/configuration`);
  await assertOk(r);
  return (await r.json()) as DiscoveryOptions;
}

export async function putNetConfiguration(
  baseUrl: string,
  body: DiscoveryOptions,
): Promise<DiscoveryOptions> {
  const r = await fetch(`${baseUrl}/api/net/configuration`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  await assertOk(r);
  return (await r.json()) as DiscoveryOptions;
}

export async function fetchLanPeers(baseUrl: string): Promise<LanPeerSnapshot[]> {
  const r = await fetch(`${baseUrl}/api/net/lan-peers`);
  await assertOk(r);
  const data = (await r.json()) as unknown;
  return Array.isArray(data) ? data : [];
}

export async function postNetDisconnect(baseUrl: string): Promise<DiscoveryOptions> {
  const r = await fetch(`${baseUrl}/api/net/disconnect`, { method: "POST" });
  await assertOk(r);
  return (await r.json()) as DiscoveryOptions;
}

export async function postConnectByIp(
  baseUrl: string,
  ipAddress: string,
): Promise<ConnectByIpResult> {
  const r = await fetch(`${baseUrl}/api/net/connect-by-ip`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ipAddress }),
  });
  await assertOk(r);
  return (await r.json()) as ConnectByIpResult;
}
