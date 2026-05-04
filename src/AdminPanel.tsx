import { useCallback, useEffect, useState } from "react";
import "./AdminPanel.css";

type NetConfiguredRole = "none" | "host" | "client";

type NetRoleResponse = {
  role: NetConfiguredRole;
};

type NetStatus = {
  configuredRole: NetConfiguredRole;
  state: string;
  thisHostIp: string | null;
  remoteHostIp: string | null;
  remoteTcpPort: number | null;
  remoteHostBaseUrl: string | null;
  lanPort: number;
  udpPort: number;
  appId: string;
};

function roleLabel(role: NetConfiguredRole): string {
  switch (role) {
    case "host":
      return "хост (beacon из appsettings)";
    case "client":
      return "клиент (поиск хоста из appsettings)";
    default:
      return "выкл. (Role: none в appsettings)";
  }
}

/** Ctrl+Shift+` (Backquote) — одна и та же комбинация открывает и закрывает. */
function isAdminToggle(e: KeyboardEvent): boolean {
  return (
    e.ctrlKey &&
    e.shiftKey &&
    !e.altKey &&
    !e.metaKey &&
    e.code === "Backquote"
  );
}

type AdminPanelProps = {
  baseUrl: string | null;
  backendLoadError: string | null;
};

export function AdminPanel({ baseUrl, backendLoadError }: AdminPanelProps) {
  const [open, setOpen] = useState(false);
  const [configuredRole, setConfiguredRole] = useState<NetConfiguredRole | null>(null);
  const [net, setNet] = useState<NetStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const toggle = useCallback(() => {
    setOpen((v) => !v);
  }, []);

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (!isAdminToggle(e)) return;
      e.preventDefault();
      toggle();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [toggle]);

  const fetchRole = useCallback(async () => {
    if (!baseUrl) return;
    const r = await fetch(`${baseUrl}/api/net/role`);
    if (!r.ok) throw new Error(`role ${r.status}`);
    const data = (await r.json()) as NetRoleResponse;
    setConfiguredRole(data.role);
  }, [baseUrl]);

  const fetchStatus = useCallback(async () => {
    if (!baseUrl) return;
    const r = await fetch(`${baseUrl}/api/net/status`);
    if (!r.ok) throw new Error(`status ${r.status}`);
    setNet((await r.json()) as NetStatus);
  }, [baseUrl]);

  useEffect(() => {
    if (!baseUrl) return;
    void fetchRole().catch((e) => setError(String(e)));
  }, [baseUrl, fetchRole]);

  useEffect(() => {
    if (!baseUrl) return;
    void fetchStatus();
    const id = window.setInterval(() => void fetchStatus().catch(() => {}), 1000);
    return () => window.clearInterval(id);
  }, [baseUrl, fetchStatus]);

  async function refreshPageInfo() {
    if (!baseUrl) return;
    setRefreshing(true);
    setError(null);
    try {
      await Promise.all([fetchRole(), fetchStatus()]);
    } catch (e) {
      setError(String(e));
    } finally {
      setRefreshing(false);
    }
  }

  return (
    <div
      className={`admin-panel ${open ? "admin-panel--open" : ""}`}
      role="region"
      aria-label="Админ панель"
      aria-hidden={!open}
    >
      <div className="admin-panel__scroll">
        <h2 className="admin-panel__title">админ панель</h2>

        {backendLoadError && <p className="error">{backendLoadError}</p>}
        {!baseUrl && !backendLoadError && <p className="muted">Загрузка backend…</p>}
        {error && <p className="error">{error}</p>}

        <section className="card admin-panel__card">
          <div className="row card-header-row">
            <h2>Режим из конфигурации</h2>
            <button
              type="button"
              className="btn-refresh"
              disabled={!baseUrl || refreshing}
              onClick={() => void refreshPageInfo()}
            >
              {refreshing ? "Обновление…" : "Обновить"}
            </button>
          </div>
          {configuredRole == null && baseUrl ? (
            <p className="muted">Запрос /api/net/role…</p>
          ) : configuredRole == null ? (
            <p className="muted">Ожидание адреса backend…</p>
          ) : (
            <>
              <p>
                <strong>{roleLabel(configuredRole)}</strong>
              </p>
              <p className="hint">
                Меняется только в <code>appsettings.json</code> → <code>Net:Role</code> (<code>none</code>,{" "}
                <code>host</code>, <code>client</code>), затем перезапуск процесса backend.
              </p>
            </>
          )}
        </section>

        {net && (
          <section className="card status admin-panel__card">
            <h2>Статус discovery</h2>
            <dl>
              <dt>configuredRole (из конфига)</dt>
              <dd>{net.configuredRole}</dd>
              <dt>state</dt>
              <dd>{net.state}</dd>
              <dt>thisHostIp</dt>
              <dd>{net.thisHostIp ?? "—"}</dd>
              <dt>remoteHostBaseUrl</dt>
              <dd>{net.remoteHostBaseUrl ?? "—"}</dd>
              <dt>UDP / LAN порты</dt>
              <dd>
                {net.udpPort} / {net.lanPort}
              </dd>
              <dt>appId</dt>
              <dd>{net.appId}</dd>
            </dl>
          </section>
        )}
      </div>
    </div>
  );
}
