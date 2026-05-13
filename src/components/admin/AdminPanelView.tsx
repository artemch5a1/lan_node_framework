import type {
  DiscoveryOptions,
  LanPeerSnapshot,
  NetConfiguredRole,
  NetStatus,
} from "../../api/types";
import type { AdminPanelTab } from "../../stores/AdminNetStore";
import "../../AdminPanel.css";

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

export type AdminPanelViewProps = {
  open: boolean;
  activeTab: AdminPanelTab;
  onTabChange: (tab: AdminPanelTab) => void;
  onOpenLanTab: () => void;
  baseUrl: string | null;
  backendLoadError: string | null;
  configuredRole: NetConfiguredRole | null;
  net: NetStatus | null;
  configuration: DiscoveryOptions | null;
  refreshing: boolean;
  savingConfiguration: boolean;
  lanLoading: boolean;
  lanPeersDisplay: LanPeerSnapshot[];
  onRefreshPageInfo: () => void;
  onScanLanPeers: () => void;
  onSaveConfiguration: () => void;
  onConnect: (ip: string) => void;
  onDisconnect: () => void;
  isConnectedToLanPeer: (ip: string) => boolean;
  onConfigurationFieldChange: <K extends keyof DiscoveryOptions>(
    key: K,
    value: DiscoveryOptions[K],
  ) => void;
};

export function AdminPanelView({
  open,
  activeTab,
  onTabChange,
  onOpenLanTab,
  baseUrl,
  backendLoadError,
  configuredRole,
  net,
  configuration,
  refreshing,
  savingConfiguration,
  lanLoading,
  lanPeersDisplay,
  onRefreshPageInfo,
  onScanLanPeers,
  onSaveConfiguration,
  onConnect,
  onDisconnect,
  isConnectedToLanPeer,
  onConfigurationFieldChange,
}: AdminPanelViewProps) {
  return (
    <div
      className={`admin-panel ${open ? "admin-panel--open" : ""}`}
      role="region"
      aria-label="Админ панель"
      aria-hidden={!open}
    >
      <div className="admin-panel__scroll">
        <h2 className="admin-panel__title">админ панель</h2>

        {!baseUrl && !backendLoadError && <p className="muted">Загрузка backend…</p>}

        <section className="admin-panel__menu admin-panel__card">
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "current" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => onTabChange("current")}
          >
            Текущая конфигурация
          </button>
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "change" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => onTabChange("change")}
          >
            Смена конфигурации
          </button>
          <button
            type="button"
            className={`admin-panel__menu-btn ${activeTab === "lan" ? "admin-panel__menu-btn--active" : ""}`}
            onClick={() => {
              onTabChange("lan");
              onOpenLanTab();
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
                  onClick={() => void onRefreshPageInfo()}
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
                onClick={() => void onSaveConfiguration()}
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
                    onChange={(e) => onConfigurationFieldChange("productSlug", e.target.value)}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>Instance slug (экземпляр, [a-z0-9])</span>
                  <input
                    type="text"
                    value={configuration.instanceSlug}
                    onChange={(e) => onConfigurationFieldChange("instanceSlug", e.target.value)}
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
                    onChange={(e) => onConfigurationFieldChange("udpPort", Number(e.target.value))}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>LanPort</span>
                  <input
                    type="number"
                    value={configuration.lanPort}
                    onChange={(e) => onConfigurationFieldChange("lanPort", Number(e.target.value))}
                  />
                </label>

                <label className="admin-panel__field">
                  <span>BeaconIntervalMs</span>
                  <input
                    type="number"
                    value={configuration.beaconIntervalMs}
                    onChange={(e) =>
                      onConfigurationFieldChange("beaconIntervalMs", Number(e.target.value))
                    }
                  />
                </label>

                <label className="admin-panel__field">
                  <span>DiscoveryTimeoutMs</span>
                  <input
                    type="number"
                    value={configuration.discoveryTimeoutMs}
                    onChange={(e) =>
                      onConfigurationFieldChange("discoveryTimeoutMs", Number(e.target.value))
                    }
                  />
                </label>

                <label className="admin-panel__field">
                  <span>ProtocolVersion</span>
                  <input
                    type="number"
                    value={configuration.protocolVersion}
                    onChange={(e) =>
                      onConfigurationFieldChange("protocolVersion", Number(e.target.value))
                    }
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
                onClick={() => void onScanLanPeers()}
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
                    <tr
                      key={
                        p.seenInDiscovery === false
                          ? `${p.ipAddress}__sticky`
                          : `${p.ipAddress}-${p.beaconName}`
                      }
                    >
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
                            onClick={() => void onDisconnect()}
                          >
                            Отключиться
                          </button>
                        ) : (
                          <button
                            type="button"
                            className="btn-refresh"
                            disabled={savingConfiguration || !configuration}
                            onClick={() => void onConnect(p.ipAddress)}
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
