import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { getBackendBaseUrl } from "../api/tauriBackendApi";
import { useNotificationService } from "../services/NotificationServiceProvider";

type BackendSessionState = {
  baseUrl: string | null;
  backendLoadError: string | null;
};

const BackendSessionContext = createContext<BackendSessionState | null>(null);

export function BackendSessionStoreProvider({ children }: { children: ReactNode }) {
  const notifications = useNotificationService();
  const [baseUrl, setBaseUrl] = useState<string | null>(null);
  const [backendLoadError, setBackendLoadError] = useState<string | null>(null);

  useEffect(() => {
    void getBackendBaseUrl()
      .then(setBaseUrl)
      .catch((e: unknown) => {
        setBackendLoadError(String(e));
        notifications.showErrorFromUnknown(e);
      });
  }, [notifications]);

  const value: BackendSessionState = { baseUrl, backendLoadError };

  return (
    <BackendSessionContext.Provider value={value}>{children}</BackendSessionContext.Provider>
  );
}

export function useBackendSession(): BackendSessionState {
  const v = useContext(BackendSessionContext);
  if (!v) throw new Error("useBackendSession must be used within BackendSessionStoreProvider");
  return v;
}
