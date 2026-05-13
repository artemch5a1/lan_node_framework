import type { ReactNode } from "react";
import { AdminNetStoreProvider } from "../stores/AdminNetStore";
import { BackendSessionStoreProvider } from "../stores/BackendSessionStore";
import { BooksStoreProvider } from "../stores/BooksStore";
import { NotificationServiceProvider } from "../services/NotificationServiceProvider";
import { ToastProvider } from "../ui/ToastProvider";

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ToastProvider>
      <NotificationServiceProvider>
        <BackendSessionStoreProvider>
          <BooksStoreProvider>
            <AdminNetStoreProvider>{children}</AdminNetStoreProvider>
          </BooksStoreProvider>
        </BackendSessionStoreProvider>
      </NotificationServiceProvider>
    </ToastProvider>
  );
}
