import {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from "react";
import "./ToastProvider.css";

type ToastItem = { id: number; message: string };

type ToastContextValue = {
  notifyError: (message: string) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);

export function useToast(): ToastContextValue {
  const v = useContext(ToastContext);
  if (!v) throw new Error("useToast must be used within ToastProvider");
  return v;
}

const TOAST_MS = 6000;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const idRef = useRef(0);

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const notifyError = useCallback(
    (message: string) => {
      const trimmed = message.trim();
      if (!trimmed) return;
      const id = ++idRef.current;
      setToasts((prev) => [...prev, { id, message: trimmed }]);
      window.setTimeout(() => dismiss(id), TOAST_MS);
    },
    [dismiss],
  );

  return (
    <ToastContext.Provider value={{ notifyError }}>
      {children}
      <div className="toast-stack" aria-live="assertive">
        {toasts.map((t) => (
          <div key={t.id} className="toast toast--error" role="alert">
            <span className="toast__text">{t.message}</span>
            <button
              type="button"
              className="toast__close"
              aria-label="Закрыть"
              onClick={() => dismiss(t.id)}
            >
              ×
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
