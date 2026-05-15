import { useCallback, useEffect } from "react";
import { useAdminNetStore } from "../../stores/AdminNetStore";
import { AdminPanelView } from "./AdminPanelView";
import { isAdminToggle } from "./adminPanelShortcuts";

export function AdminPanelContainer() {
  const store = useAdminNetStore();
  const { open, setOpen, setActiveTab, scanLanPeers, baseUrl } = store;

  const toggle = useCallback(() => {
    setOpen((v) => !v);
  }, [setOpen]);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (!isAdminToggle(e)) return;
      e.preventDefault();
      toggle();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [toggle]);

  useEffect(() => {
    if (!open || !baseUrl) return;
    void scanLanPeers();
  }, [open, baseUrl, scanLanPeers]);

  return (
    <AdminPanelView
      open={open}
      activeTab={store.activeTab}
      onTabChange={setActiveTab}
      baseUrl={store.baseUrl}
      backendLoadError={store.backendLoadError}
      configuredRole={store.configuredRole}
      net={store.net}
      configuration={store.configuration}
      refreshing={store.refreshing}
      savingConfiguration={store.savingConfiguration}
      lanLoading={store.lanLoading}
      lanPeersDisplay={store.lanPeersDisplay}
      onRefreshPageInfo={store.refreshPageInfo}
      onScanLanPeers={store.scanLanPeers}
      onSaveConfiguration={store.saveConfiguration}
      onConnect={store.connectToLanPeer}
      onConnectByManualIp={store.connectByManualIp}
      onDisconnect={store.disconnectFromRemoteHost}
      isConnectedToLanPeer={store.isConnectedToLanPeer}
      onConfigurationFieldChange={store.updateConfigurationField}
    />
  );
}
