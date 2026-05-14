export type Book = {
  id: number;
  title: string;
  author: string;
  yearPublished: number;
};

export type NetConfiguredRole = "none" | "host" | "client";

export type NetRoleResponse = {
  role: NetConfiguredRole;
};

export type NetLocalIpv4Endpoint = {
  address: string;
  interfaceDescription: string;
};

export type NetStatus = {
  configuredRole: NetConfiguredRole;
  state: string;
  thisHostIp: string | null;
  remoteHostIp: string | null;
  remoteTcpPort: number | null;
  remoteHostBaseUrl: string | null;
  lanPort: number;
  udpPort: number;
  appId: string;
  productSlug: string;
  instanceSlug: string;
  instanceGuid: string;
  /** Локальные IPv4 с подписью адаптера (для выбора адреса в реальной LAN). */
  localIpv4Endpoints: NetLocalIpv4Endpoint[];
};

export type DiscoveryOptions = {
  role: NetConfiguredRole;
  appId: string;
  productSlug: string;
  instanceSlug: string;
  instanceGuid: string;
  remoteHostIp: string | null;
  udpPort: number;
  lanPort: number;
  beaconIntervalMs: number;
  discoveryTimeoutMs: number;
  protocolVersion: number;
};

export type LanPeerSnapshot = {
  ipAddress: string;
  beaconName: string;
  productSlug: string;
  instanceSlug: string;
  /** false — сохранённое подключение, в эфире сейчас не видно */
  seenInDiscovery?: boolean;
};

export type ConnectByIpResult = {
  configuration: DiscoveryOptions;
  peer: LanPeerSnapshot;
};
