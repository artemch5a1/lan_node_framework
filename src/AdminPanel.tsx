import { useCallback, useEffect, useMemo, useState } from "react";
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
  productSlug: string;
  instanceSlug: string;
  instanceGuid: string;
};

type DiscoveryOptions = {
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

type LanPeerSnapshot = {
  ipAddress: string;
  beaconName: string;
  productSlug: string;
  instanceSlug: string;
  /** false — сохранённое подключение, в эфире сейчас не видно */
  seenInDiscovery?: boolean;
};

/** Пока нет ответа сканирования — показываем текущий remote, чтобы можно было отключиться. */
function mergeLanPeersWithStickyConnection(
  peers: LanPeerSnapshot[],
  net: NetStatus | null,
): LanPeerSnapshot[] {
  if (!net || net.configuredRole !== "client" || !net.remoteHostIp) return peers;
  const ip = net.remoteHostIp.trim();
  if (!ip || peers.some((p) => p.ipAddress === ip)) return peers;
  return [
    {
      ipAddress: ip,
      beaconName: "—",
      productSlug: net.productSlug ?? "",
      instanceSlug: "(нет в эфире)",
      seenInDiscovery: false,
    },
    ...peers,
  ];
}

type AdminPanelTab = "current" | "change" | "lan";

function roleLabel(role: NetConfiguredRole): string {
  switch (role) {
    case "host":
      return "хост (вещание в LAN)";
    case "client":
      return "клиент (подключение к удалённому узлу)";
    default:
      return "выкл.";
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
  const [activeTab, setActiveTab] = useState<AdminPanelTab>("current");
  const [configuredRole, setConfiguredRole] = useState<NetConfiguredRole | null>(null);
  const [net, setNet] = useState<NetStatus | null>(null);
  const [configuration, setConfiguration] = useState<DiscoveryOptions | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [savingConfiguration, setSavingConfiguration] = useState(false);
  const [lanPeers, setLanPeers] = useState<LanPeerSnapshot[]>([]);
  const [lanLoading, setLanLoading] = useState(false);

  const lanPeersDisplay = useMemo(
    () => mergeLanPeersWithStickyConnection(lanPeers, net),
    [lanPeers, net],
  );

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

  const fetchConfiguration = useCallback(async () => {
    if (!baseUrl) return;
    const r = await fetch(`${baseUrl}/api/net/configuration`);
    if (!r.ok) throw new Error(`configuration ${r.status}`);
    setConfiguration((await r.json()) as DiscoveryOptions);
  }, [baseUrl]);

  useEffect(() => {
    if (!baseUrl) return;
    void fetchRole().catch((e) => setError(String(e)));
    void fetchConfiguration().catch((e) => setError(String(e)));
  }, [baseUrl, fetchRole, fetchConfiguration]);

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

  async function scanLanPeers() {
    if (!baseUrl) return;
    setLanLoading(true);
    setError(null);
    try {
      const response = await fetch(`${baseUrl}/api/net/lan-peers`);
      if (!response.ok) throw new Error(`lan-peers ${response.status}`);
      const data = (await response.json()) as LanPeerSnapshot[];
      setLanPeers(Array.isArray(data) ? data : []);
    } catch (e) {
      setError(String(e));
    } finally {
      setLanLoading(false);
    }
  }

  function isConnectedToLanPeer(ip: string): boolean {
    return net?.configuredRole === "client" && net?.remoteHostIp === ip;
  }

  async function connectToLanPeer(ip: string) {
    if (!baseUrl || !configuration) return;
    setSavingConfiguration(true);
    setError(null);
    try {
      const body: DiscoveryOptions = {
        ...configuration,
        role: "client",
        remoteHostIp: ip,
      };
      const response = await fetch(`${baseUrl}/api/net/configuration`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      if (!response.ok) throw new Error(`configuration update ${response.status}`);
      const updatedConfiguration = (await response.json()) as DiscoveryOptions;
      setConfiguration(updatedConfiguration);
      await Promise.all([fetchRole(), fetchStatus(), scanLanPeers()]);
    } catch (e) {
      setError(String(e));
    } finally {
      setSavingConfiguration(false);
    }
  }

  async function disconnectFromRemoteHost() {
    if (!baseUrl) return;
    setSavingConfiguration(true);
    setError(null);
    try {
      const response = await fetch(`${baseUrl}/api/net/disconnect`, { method: "POST" });
      if (!response.ok) throw new Error(`disconnect ${response.status}`);
      const updatedConfiguration = (await response.json()) as DiscoveryOptions;
      setConfiguration(updatedConfiguration);
      await Promise.all([fetchRole(), fetchStatus(), scanLanPeers()]);
    } catch (e) {
      setError(String(e));
    } finally {
      setSavingConfiguration(false);
    }
  }

  async function saveConfiguration() {
    if (!baseUrl || !configuration) return;
    setSavingConfiguration(true);
    setError(null);
    try {
      const response = await fetch(`${baseUrl}/api/net/configuration`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(configuration),
      });

      if (!response.ok) throw new Error(`configuration update ${response.status}`);

      const updatedConfiguration = (await response.json()) as DiscoveryOptions;
      setConfiguration(updatedConfiguration);
      await Promise.all([fetchRole(), fetchStatus()]);
    } catch (e) {
      setError(String(e));
    } finally {
      setSavingConfiguration(false);
    }
  }

  function updateConfigurationField<K extends keyof DiscoveryOptions>(
    key: K,
    value: DiscoveryOptions[K]
  ) {
    setConfiguration((prev) => (prev ? { ...prev, [key]: value } : prev));
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

        <section className="admin-panel__menu admin-panel__card">
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "current" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => setActiveTab("current")}
          >
            Текущая конфигурация
          </button>
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "change" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => setActiveTab("change")}
          >
            Смена конфигурации
          </button>
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "lan" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => {
              setActiveTab("lan");
              void scanLanPeers();
            }}
          >
            LAN
          </button>
        </section>

        {activeTab === "current" && (
          <>
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
                    Роль host/client выставляется автоматически: по умолчанию хост; при выборе узла в
                    вкладке LAN — клиент; отключение — снова хост.
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
                  <dt>appId (beacon)</dt>
                  <dd>{net.appId}</dd>
                  <dt>productSlug</dt>
                  <dd>{net.productSlug || "—"}</dd>
                  <dt>instanceSlug</dt>
                  <dd>{net.instanceSlug || "—"}</dd>
                  <dt>instanceGuid</dt>
                  <dd>{net.instanceGuid || "—"}</dd>
                </dl>
              </section>
            )}
          </>
        )}

        {activeTab === "change" && (
          <section className="card admin-panel__card">
            <div className="row card-header-row">
              <h2>Изменение конфигурации</h2>
              <button
                type="button"
                className="btn-refresh"
                disabled={!baseUrl || !configuration || savingConfiguration}
                onClick={() => void saveConfiguration()}
              >
                {savingConfiguration ? "Сохранение…" : "Сохранить"}
              </button>
            </div>
            {!configuration ? (
              <p className="muted">Загрузка текущей конфигурации…</p>
            ) : (
              <div className="admin-panel__form">
                <p className="hint">
                  Роль host/client выставляется только автоматически: по умолчанию хост; подключение к
                  узлу из вкладки LAN — клиент; «Отключиться» там же — снова хост.
                </p>
                <label className="admin-panel__field">
                  <span>Product slug (общий для линейки)</span>
                  <input
                    type="text"
                    value={configuration.productSlug}
                    onChange={(e) => updateConfigurationField("productSlug", e.target.value)}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>Instance slug (экземпляр, [a-z0-9])</span>
                  <input
                    type="text"
                    value={configuration.instanceSlug}
                    onChange={(e) => updateConfigurationField("instanceSlug", e.target.value)}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>Beacon AppId (пересобирается при сохранении)</span>
                  <input type="text" readOnly value={configuration.appId} />
                </label>

                <label className="admin-panel__field">
                  <span>UdpPort</span>
                  <input
                    type="number"
                    value={configuration.udpPort}
                    onChange={(e) => updateConfigurationField("udpPort", Number(e.target.value))}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>LanPort</span>
                  <input
                    type="number"
                    value={configuration.lanPort}
                    onChange={(e) => updateConfigurationField("lanPort", Number(e.target.value))}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>BeaconIntervalMs</span>
                  <input
                    type="number"
                    value={configuration.beaconIntervalMs}
                    onChange={(e) =>
                      updateConfigurationField("beaconIntervalMs", Number(e.target.value))
                    }
                  />
                </label>

                <label className="admin-panel__field">
                  <span>DiscoveryTimeoutMs</span>
                  <input
                    type="number"
                    value={configuration.discoveryTimeoutMs}
                    onChange={(e) =>
                      updateConfigurationField("discoveryTimeoutMs", Number(e.target.value))
                    }
                  />
                </label>

                <label className="admin-panel__field">
                  <span>ProtocolVersion</span>
                  <input
                    type="number"
                    value={configuration.protocolVersion}
                    onChange={(e) => updateConfigurationField("protocolVersion", Number(e.target.value))}
                  />
                </label>
              </div>
            )}
          </section>
        )}

        {activeTab === "lan" && (
          <section className="card admin-panel__card">
            <div className="row card-header-row">
              <h2>Узлы в LAN (тот же productSlug)</h2>
              <button
                type="button"
                className="btn-refresh"
                disabled={!baseUrl || lanLoading}
                onClick={() => void scanLanPeers()}
              >
                {lanLoading ? "Сканирование…" : "Обновить"}
              </button>
            </div>
            <p className="hint">
              Формат beacon: <code>DLSv1-&lt;productSlug&gt;-&lt;instanceSlug&gt;</code>. Собственный узел в
              списке не показывается. Текущее подключение остаётся в таблице даже без beacon (строка «нет
              в эфире»), чтобы можно было отключиться. «Подключиться» / «Отключиться» — как раньше.
            </p>
            {lanLoading && lanPeersDisplay.length === 0 ? (
              <p className="muted">Сканирование…</p>
            ) : lanPeersDisplay.length === 0 ? (
              <p className="muted">Узлы не найдены или список ещё не запрашивался.</p>
            ) : (
              <table className="admin-panel__table">
                <thead>
                  <tr>
                    <th>IP</th>
                    <th>Экземпляр</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {lanPeersDisplay.map((p) => (
                    <tr key={p.seenInDiscovery === false ? `${p.ipAddress}__sticky` : `${p.ipAddress}-${p.beaconName}`}>
                      <td>{p.ipAddress}</td>
                      <td>
                        <code>{p.instanceSlug}</code>
                        <span className="muted"> ({p.beaconName})</span>
                      </td>
                      <td>
                        {isConnectedToLanPeer(p.ipAddress) ? (
                          <button
                            type="button"
                            className="btn-refresh"
                            disabled={savingConfiguration}
                            onClick={() => void disconnectFromRemoteHost()}
                          >
                            Отключиться
                          </button>
                        ) : (
                          <button
                            type="button"
                            className="btn-refresh"
                            disabled={savingConfiguration || !configuration}
                            onClick={() => void connectToLanPeer(p.ipAddress)}
                          >
                            Подключиться
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        )}
      </div>
    </div>
  );
}
