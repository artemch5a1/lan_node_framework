import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
} from "react";
import {
  fetchLanPeers,
  fetchNetConfiguration,
  fetchNetRole,
  fetchNetStatus,
  postConnectByIp,
  postNetDisconnect,
  putNetConfiguration,
} from "../api/netApi";
import type { DiscoveryOptions, LanPeerSnapshot, NetConfiguredRole, NetStatus } from "../api/types";
import { mergeLanPeersWithStickyConnection } from "../domain/mergeLanPeers";
import { useNotificationService } from "../services/NotificationServiceProvider";
import { useBackendSession } from "./BackendSessionStore";

export type AdminPanelTab = "current" | "change";

type AdminNetStoreValue = {
  baseUrl: string | null;
  backendLoadError: string | null;
  open: boolean;
  setOpen: Dispatch<SetStateAction<boolean>>;
  activeTab: AdminPanelTab;
  setActiveTab: Dispatch<SetStateAction<AdminPanelTab>>;
  configuredRole: NetConfiguredRole | null;
  net: NetStatus | null;
  configuration: DiscoveryOptions | null;
  refreshing: boolean;
  savingConfiguration: boolean;
  lanLoading: boolean;
  lanPeersDisplay: LanPeerSnapshot[];
  refreshPageInfo: () => Promise<void>;
  scanLanPeers: () => Promise<void>;
  isConnectedToLanPeer: (ip: string) => boolean;
  connectToLanPeer: (ip: string) => Promise<void>;
  disconnectFromRemoteHost: () => Promise<void>;
  connectByManualIp: (ip: string) => Promise<boolean>;
  saveConfiguration: () => Promise<void>;
  updateConfigurationField: <K extends keyof DiscoveryOptions>(
    key: K,
    value: DiscoveryOptions[K],
  ) => void;
};

const AdminNetStoreContext = createContext<AdminNetStoreValue | null>(null);

export function AdminNetStoreProvider({ children }: { children: ReactNode }) {
  const { baseUrl, backendLoadError } = useBackendSession();
  const notifications = useNotificationService();

  const [open, setOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<AdminPanelTab>("current");
  const [configuredRole, setConfiguredRole] = useState<NetConfiguredRole | null>(null);
  const [net, setNet] = useState<NetStatus | null>(null);
  const [configuration, setConfiguration] = useState<DiscoveryOptions | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [savingConfiguration, setSavingConfiguration] = useState(false);
  const [lanPeers, setLanPeers] = useState<LanPeerSnapshot[]>([]);
  const [lanLoading, setLanLoading] = useState(false);
  const [manualLanOverlay, setManualLanOverlay] = useState<LanPeerSnapshot | null>(null);

  const lanPeersDisplay = useMemo(
    () => mergeLanPeersWithStickyConnection(lanPeers, net, manualLanOverlay),
    [lanPeers, net, manualLanOverlay],
  );

  const loadRole = useCallback(async () => {
    if (!baseUrl) return;
    const data = await fetchNetRole(baseUrl);
    setConfiguredRole(data.role);
  }, [baseUrl]);

  const loadStatus = useCallback(async () => {
    if (!baseUrl) return;
    const data = await fetchNetStatus(baseUrl);
    setNet(data);
  }, [baseUrl]);

  const loadConfiguration = useCallback(async () => {
    if (!baseUrl) return;
    const data = await fetchNetConfiguration(baseUrl);
    setConfiguration(data);
  }, [baseUrl]);

  useEffect(() => {
    if (!baseUrl) return;
    void loadRole().catch((e) => notifications.showErrorFromUnknown(e));
    void loadConfiguration().catch((e) => notifications.showErrorFromUnknown(e));
  }, [baseUrl, loadRole, loadConfiguration, notifications]);

  useEffect(() => {
    if (!baseUrl) return;
    void loadStatus();
    const id = window.setInterval(() => {
      void loadStatus().catch(() => {});
    }, 1000);
    return () => window.clearInterval(id);
  }, [baseUrl, loadStatus]);

  const refreshPageInfo = useCallback(async () => {
    if (!baseUrl) return;
    setRefreshing(true);
    try {
      await Promise.all([loadRole(), loadStatus()]);
    } catch (e) {
      notifications.showErrorFromUnknown(e);
    } finally {
      setRefreshing(false);
    }
  }, [baseUrl, loadRole, loadStatus, notifications]);

  const scanLanPeers = useCallback(async () => {
    if (!baseUrl) return;
    setLanLoading(true);
    try {
      const data = await fetchLanPeers(baseUrl);
      setLanPeers(data);
    } catch (e) {
      notifications.showErrorFromUnknown(e);
    } finally {
      setLanLoading(false);
    }
  }, [baseUrl, notifications]);

  const isConnectedToLanPeer = useCallback(
    (ip: string) =>
      net?.configuredRole === "client" &&
      (net?.remoteHostIp?.trim() ?? "") === (ip?.trim() ?? ""),
    [net],
  );

  const connectToLanPeer = useCallback(
    async (ip: string) => {
      if (!baseUrl || !configuration) return;
      setSavingConfiguration(true);
      try {
        const body: DiscoveryOptions = {
          ...configuration,
          role: "client",
          remoteHostIp: ip,
        };
        const updated = await putNetConfiguration(baseUrl, body);
        setConfiguration(updated);
        setManualLanOverlay(null);
        await Promise.all([loadRole(), loadStatus(), scanLanPeers()]);
      } catch (e) {
        notifications.showErrorFromUnknown(e);
      } finally {
        setSavingConfiguration(false);
      }
    },
    [baseUrl, configuration, loadRole, loadStatus, scanLanPeers, notifications],
  );

  const disconnectFromRemoteHost = useCallback(async () => {
    if (!baseUrl) return;
    setSavingConfiguration(true);
    try {
      const updated = await postNetDisconnect(baseUrl);
      setConfiguration(updated);
      setManualLanOverlay(null);
      await Promise.all([loadRole(), loadStatus(), scanLanPeers()]);
    } catch (e) {
      notifications.showErrorFromUnknown(e);
    } finally {
      setSavingConfiguration(false);
    }
  }, [baseUrl, loadRole, loadStatus, scanLanPeers, notifications]);

  const saveConfiguration = useCallback(async () => {
    if (!baseUrl || !configuration) return;
    setSavingConfiguration(true);
    try {
      const updated = await putNetConfiguration(baseUrl, configuration);
      setConfiguration(updated);
      await Promise.all([loadRole(), loadStatus()]);
    } catch (e) {
      notifications.showErrorFromUnknown(e);
    } finally {
      setSavingConfiguration(false);
    }
  }, [baseUrl, configuration, loadRole, loadStatus, notifications]);

  const updateConfigurationField = useCallback(
    <K extends keyof DiscoveryOptions>(key: K, value: DiscoveryOptions[K]) => {
      setConfiguration((prev) => (prev ? { ...prev, [key]: value } : prev));
    },
    [],
  );

  const connectByManualIp = useCallback(async (ip: string): Promise<boolean> => {
    if (!baseUrl) return false;
    const trimmed = ip.trim();
    if (!trimmed) return false;
    setSavingConfiguration(true);
    try {
      const result = await postConnectByIp(baseUrl, trimmed);
      setConfiguration(result.configuration);
      setManualLanOverlay({
        ...result.peer,
        beaconName: result.peer.beaconName || "—",
        productSlug: result.peer.productSlug ?? "",
        instanceSlug: result.peer.instanceSlug ?? "",
        seenInDiscovery: result.peer.seenInDiscovery ?? false,
      });
      await Promise.all([loadRole(), loadStatus(), scanLanPeers()]);
      return true;
    } catch (e) {
      notifications.showErrorFromUnknown(e);
      return false;
    } finally {
      setSavingConfiguration(false);
    }
  }, [baseUrl, loadRole, loadStatus, scanLanPeers, notifications]);

  const value: AdminNetStoreValue = useMemo(
    () => ({
      baseUrl,
      backendLoadError,
      open,
      setOpen,
      activeTab,
      setActiveTab,
      configuredRole,
      net,
      configuration,
      refreshing,
      savingConfiguration,
      lanLoading,
      lanPeersDisplay,
      refreshPageInfo,
      scanLanPeers,
      isConnectedToLanPeer,
      connectToLanPeer,
      disconnectFromRemoteHost,
      connectByManualIp,
      saveConfiguration,
      updateConfigurationField,
    }),
    [
      baseUrl,
      backendLoadError,
      open,
      activeTab,
      configuredRole,
      net,
      configuration,
      refreshing,
      savingConfiguration,
      lanLoading,
      lanPeersDisplay,
      refreshPageInfo,
      scanLanPeers,
      isConnectedToLanPeer,
      connectToLanPeer,
      disconnectFromRemoteHost,
      connectByManualIp,
      saveConfiguration,
      updateConfigurationField,
    ],
  );

  return (
    <AdminNetStoreContext.Provider value={value}>{children}</AdminNetStoreContext.Provider>
  );
}

export function useAdminNetStore(): AdminNetStoreValue {
  const v = useContext(AdminNetStoreContext);
  if (!v) throw new Error("useAdminNetStore must be used within AdminNetStoreProvider");
  return v;
}
