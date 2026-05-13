import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useToast } from "../ui/ToastProvider";
import { createNotificationService, type AppNotificationService } from "./notificationService";

const NotificationServiceContext = createContext<AppNotificationService | null>(null);

export function NotificationServiceProvider({ children }: { children: ReactNode }) {
  const { notifyError } = useToast();
  const service = useMemo(
    () => createNotificationService({ showError: notifyError }),
    [notifyError],
  );
  return (
    <NotificationServiceContext.Provider value={service}>
      {children}
    </NotificationServiceContext.Provider>
  );
}

export function useNotificationService(): AppNotificationService {
  const v = useContext(NotificationServiceContext);
  if (!v) throw new Error("useNotificationService must be used within NotificationServiceProvider");
  return v;
}
