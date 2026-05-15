import { postNetDisconnect } from "./netApi";
import {
  NET_PROXY_CHAIN_NOT_ALLOWED,
  parseNetErrorResponse,
  tryParseNetErrorCode,
} from "../services/notificationFormatting";

const PROXY_CHAIN_RECOVERED_EVENT = "dls-proxy-chain-recovered";

let proxyChainRecoveryInFlight = false;

export function subscribeProxyChainRecovered(handler: () => void): () => void {
  const listener = () => handler();
  window.addEventListener(PROXY_CHAIN_RECOVERED_EVENT, listener);
  return () => window.removeEventListener(PROXY_CHAIN_RECOVERED_EVENT, listener);
}

async function recoverFromProxyChain(baseUrl: string): Promise<void> {
  if (proxyChainRecoveryInFlight) return;
  proxyChainRecoveryInFlight = true;
  try {
    await postNetDisconnect(baseUrl);
    window.dispatchEvent(new Event(PROXY_CHAIN_RECOVERED_EVENT));
  } catch {
    /* disconnect is best-effort */
  } finally {
    proxyChainRecoveryInFlight = false;
  }
}

async function handleProxyChainIfNeeded(baseUrl: string, response: Response): Promise<Response> {
  if (response.status !== 508) return response;

  const code = await tryParseNetErrorCode(response);
  if (code !== NET_PROXY_CHAIN_NOT_ALLOWED) return response;

  await recoverFromProxyChain(baseUrl);
  throw new Error(await parseNetErrorResponse(response));
}

/** HTTP к локальному backend с автоматическим disconnect при цепочке прокси. */
export async function fetchBackend(
  baseUrl: string,
  input: string,
  init?: RequestInit,
): Promise<Response> {
  const response = await fetch(input, init);
  return handleProxyChainIfNeeded(baseUrl, response);
}
