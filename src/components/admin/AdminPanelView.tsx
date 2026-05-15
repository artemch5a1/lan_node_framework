import { useEffect, useState } from "react";
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

function connectionSummary(
  net: NetStatus | null,
  configuredRole: NetConfiguredRole | null,
): string {
  const role = configuredRole ?? net?.configuredRole ?? "none";
  if (role === "client") {
    const ip = net?.remoteHostIp?.trim();
    const url = net?.remoteHostBaseUrl?.trim();
    if (ip) return `Подключены к компьютеру ${ip}`;
    if (url) return `Подключены по адресу ${url}`;
    return "Подключение к другому компьютеру";
  }
  return "Локальная работа";
}

function LanPeersSimpleList({
  peers,
  lanLoading,
  baseUrl,
  configuration,
  savingConfiguration,
  onScanLanPeers,
  onConnect,
  onConnectByManualIp,
  onDisconnect,
  isConnectedToLanPeer,
}: {
  peers: LanPeerSnapshot[];
  lanLoading: boolean;
  baseUrl: string | null;
  configuration: DiscoveryOptions | null;
  savingConfiguration: boolean;
  onScanLanPeers: () => void;
  onConnect: (ip: string) => void;
  onConnectByManualIp: (ip: string) => Promise<boolean>;
  onDisconnect: () => void;
  isConnectedToLanPeer: (ip: string) => boolean;
}) {
  const [manualIp, setManualIp] = useState("");

  function titleFor(p: LanPeerSnapshot): string {
    const slug = (p.instanceSlug ?? "").trim();
    return slug.length > 0 ? slug : "Компьютер в сети";
  }

  return (
    <>
      <div className="row card-header-row">
        <h2 className="admin-panel__section-title">Серверы рядом</h2>
        <button
          type="button"
          className="btn-refresh"
          disabled={!baseUrl || lanLoading}
          onClick={() => void onScanLanPeers()}
        >
          {lanLoading ? "Поиск…" : "Обновить список"}
        </button>
      </div>
      <p className="admin-panel__hint-soft">
        Узлы с этой же программой в вашей локальной сети.
      </p>
      {lanLoading && peers.length === 0 ? (
        <p className="muted">Ищем компьютеры…</p>
      ) : peers.length === 0 ? (
        <p className="muted">Пока никого нет. Обновите список или проверьте Wi‑Fi.</p>
      ) : (
        <ul className="admin-panel__peer-list">
          {peers.map((p) => {
            const connected = isConnectedToLanPeer(p.ipAddress);
            const offline = p.seenInDiscovery === false;
            return (
              <li
                key={
                  offline
                    ? `${p.ipAddress}__sticky`
                    : `${p.ipAddress}-${p.beaconName || "beacon"}`
                }
                className={`admin-panel__peer-item${connected ? " admin-panel__peer-item--active" : ""}`}
              >
                <div className="admin-panel__peer-main">
                  <span className="admin-panel__peer-title">{titleFor(p)}</span>
                  <span className="admin-panel__peer-ip">{p.ipAddress}</span>
                  {offline ? <span className="admin-panel__peer-pill">вне сети</span> : null}
                </div>
                <div className="admin-panel__peer-actions">
                  {connected ? (
                    <button
                      type="button"
                      className="admin-panel__btn-soft"
                      disabled={savingConfiguration}
                      onClick={() => void onDisconnect()}
                    >
                      Отключиться
                    </button>
                  ) : (
                    <button
                      type="button"
                      className="admin-panel__btn-soft admin-panel__btn-soft--primary"
                      disabled={savingConfiguration || !configuration}
                      onClick={() => void onConnect(p.ipAddress)}
                    >
                      Подключиться
                    </button>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="admin-panel__manual-ip">
        <p className="admin-panel__hint-soft admin-panel__manual-ip-title">
          Не видите сервер в списке? Введите его IP — мы проверим узел и подключим.
        </p>
        <div className="admin-panel__manual-ip-row">
          <input
            type="text"
            className="admin-panel__manual-ip-input"
            placeholder="например, 192.168.1.10"
            value={manualIp}
            onChange={(e) => setManualIp(e.target.value)}
            disabled={!baseUrl || savingConfiguration}
            autoComplete="off"
            inputMode="decimal"
          />
          <button
            type="button"
            className="admin-panel__btn-soft admin-panel__btn-soft--primary"
            disabled={!baseUrl || savingConfiguration || !manualIp.trim()}
            onClick={() =>
              void (async () => {
                const ok = await onConnectByManualIp(manualIp);
                if (ok) setManualIp("");
              })()
            }
          >
            {savingConfiguration ? "Подключение…" : "Подключить по IP"}
          </button>
        </div>
      </div>
    </>
  );
}

export type AdminPanelViewProps = {
  open: boolean;
  activeTab: AdminPanelTab;
  onTabChange: (tab: AdminPanelTab) => void;
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
  onConnectByManualIp: (ip: string) => Promise<boolean>;
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
  onConnectByManualIp,
  onDisconnect,
  isConnectedToLanPeer,
  onConfigurationFieldChange,
}: AdminPanelViewProps) {
  const [advancedOpen, setAdvancedOpen] = useState(false);

  const quickRefresh = () => {
    void onRefreshPageInfo();
    void onScanLanPeers();
  };

  useEffect(() => {
    if (!open) setAdvancedOpen(false);
  }, [open]);

  return (
    <>
      <div
        className={`admin-panel__backdrop ${open ? "admin-panel__backdrop--open" : ""}`}
        aria-hidden={!open}
      />
      <div
        className={`admin-panel ${open ? "admin-panel--open" : ""}`}
        role="region"
        aria-label="Настройки сети"
        aria-hidden={!open}
      >
      <div className="admin-panel__scroll">
        <div className="admin-panel__title-row">
          <div className="admin-panel__title-slot" />
          <h2 className="admin-panel__title">сеть</h2>
          <div className="admin-panel__title-actions">
            {advancedOpen ? (
              <button
                type="button"
                className="admin-panel__icon-btn admin-panel__icon-btn--text"
                onClick={() => setAdvancedOpen(false)}
              >
                Свернуть
              </button>
            ) : (
              <button
                type="button"
                className="admin-panel__icon-btn"
                onClick={() => {
                  setAdvancedOpen(true);
                  onTabChange("current");
                }}
                aria-label="Дополнительные настройки"
                title="Дополнительные настройки"
              >
                <svg
                  className="admin-panel__gear-icon"
                  width="22"
                  height="22"
                  viewBox="0 0 24 24"
                  aria-hidden
                >
                  <path
                    fill="currentColor"
                    d="M19.43 12.98c.04-.32.07-.64.07-.97 0-.33-.03-.66-.07-.98l2.11-1.65c.19-.15.24-.42.12-.64l-2-3.46c-.12-.22-.39-.3-.61-.22l-2.49 1c-.52-.4-1.08-.73-1.69-.98l-.38-2.65C14.46 2.18 14.25 2 14 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1c-.22-.09-.49 0-.61.22l-2 3.46c-.13.22-.07.49.12.64l2.11 1.65c-.04.32-.07.65-.07.98s.03.66.07.98l-2.11 1.65c-.19.15-.24.42-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.59 1.69-.98l2.49 1c.22.08.49 0 .61-.22l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.65zM12 15.5c-1.93 0-3.5-1.57-3.5-3.5S10.07 8.5 12 8.5s3.5 1.57 3.5 3.5-1.57 3.5-3.5 3.5z"
                  />
                </svg>
              </button>
            )}
          </div>
        </div>

        {!baseUrl && !backendLoadError && <p className="muted">Загрузка backend…</p>}
        {backendLoadError && <p className="muted">{backendLoadError}</p>}

        {!advancedOpen && (
          <>
            <section className="card admin-panel__card">
              <div className="row card-header-row">
                <h2 className="admin-panel__section-title">Ваше имя в сети</h2>
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
                <p className="muted">Загрузка…</p>
              ) : (
                <label className="admin-panel__field admin-panel__field--compact">
                  <span className="admin-panel__hint-soft">
                    Так вас увидят другие (латинские буквы и цифры).
                  </span>
                  <input
                    type="text"
                    value={configuration.instanceSlug}
                    onChange={(e) => onConfigurationFieldChange("instanceSlug", e.target.value)}
                    autoComplete="off"
                    placeholder="например, офис-pc"
                  />
                </label>
              )}
            </section>

            <section className="card admin-panel__card">
              <div className="row card-header-row">
                <h2 className="admin-panel__section-title">Обзор</h2>
                <button
                  type="button"
                  className="btn-refresh"
                  disabled={!baseUrl || refreshing || lanLoading}
                  onClick={quickRefresh}
                >
                  {refreshing || lanLoading ? "Обновление…" : "Обновить"}
                </button>
              </div>
              <p className="admin-panel__lede">{connectionSummary(net, configuredRole)}</p>
              <p className="admin-panel__hint-soft admin-panel__hint-soft--below-lede">
                Для подключения с другого ПК в обычной сети выберите адрес Wi‑Fi или Ethernet, а не
                виртуального адаптера (VirtualBox, Hyper‑V и т.п.), если вы не в этой виртуальной сети.
              </p>
              {net?.localIpv4Endpoints && net.localIpv4Endpoints.length > 0 ? (
                <ul className="admin-panel__ip-list">
                  {net.localIpv4Endpoints.map((e) => (
                    <li key={e.address} className="admin-panel__ip-item">
                      <span className="admin-panel__meta-value">{e.address}</span>
                      <span className="admin-panel__hint-soft">{e.interfaceDescription}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="admin-panel__meta-line">
                  <span className="admin-panel__meta-label">Ваш адрес</span>
                  <span className="admin-panel__meta-value">
                    {net?.thisHostIp?.trim() || "—"}
                  </span>
                </p>
              )}
            </section>

            <section className="card admin-panel__card">
              <LanPeersSimpleList
                peers={lanPeersDisplay}
                lanLoading={lanLoading}
                baseUrl={baseUrl}
                configuration={configuration}
                savingConfiguration={savingConfiguration}
                onScanLanPeers={onScanLanPeers}
                onConnect={onConnect}
                onConnectByManualIp={onConnectByManualIp}
                onDisconnect={onDisconnect}
                isConnectedToLanPeer={isConnectedToLanPeer}
              />
            </section>
          </>
        )}

        {advancedOpen && (
          <>
            <section className="admin-panel__menu admin-panel__card">
              <button
                type="button"
                className={`admin-panel__menu-btn ${activeTab === "current" ? "admin-panel__menu-btn--active" : ""}`}
                onClick={() => onTabChange("current")}
              >
                Статус
              </button>
              <button
                type="button"
                className={`admin-panel__menu-btn ${activeTab === "change" ? "admin-panel__menu-btn--active" : ""}`}
                onClick={() => onTabChange("change")}
              >
                Все параметры
              </button>
            </section>

        {activeTab === "current" && (
          <>
            <section className="card admin-panel__card">
              <div className="row card-header-row">
                <h2>Режим</h2>
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
                <p className="muted">Загрузка…</p>
              ) : configuredRole == null ? (
                <p className="muted">Ожидание backend…</p>
              ) : (
                <>
                  <p>
                    <strong>{roleLabel(configuredRole)}</strong>
                  </p>
                  <p className="hint">
                    Обычно вы сами «хост»; при подключении к другому компьютеру из списка на главном
                    экране включается режим клиента.
                  </p>
                </>
              )}
            </section>

            {net && (
              <section className="card status admin-panel__card">
                <h2>Технический статус</h2>
                <dl className="admin-panel__tech-dl">
                  <dt>Роль</dt>
                  <dd>{net.configuredRole}</dd>
                  <dt>Состояние</dt>
                  <dd>{net.state}</dd>
                  <dt>IP (основной)</dt>
                  <dd>{net.thisHostIp ?? "—"}</dd>
                  <dt>Локальные IPv4</dt>
                  <dd>
                    {net.localIpv4Endpoints?.length ? (
                      <ul className="admin-panel__ip-list admin-panel__ip-list--inline">
                        {net.localIpv4Endpoints.map((e) => (
                          <li key={e.address} className="admin-panel__ip-item">
                            <span className="admin-panel__meta-value">{e.address}</span>
                            <span className="admin-panel__hint-soft">{e.interfaceDescription}</span>
                          </li>
                        ))}
                      </ul>
                    ) : (
                      "—"
                    )}
                  </dd>
                  <dt>Удалённый узел</dt>
                  <dd>{net.remoteHostBaseUrl ?? "—"}</dd>
                  <dt>Порты UDP / LAN</dt>
                  <dd>
                    {net.udpPort} / {net.lanPort}
                  </dd>
                  <dt>Beacon</dt>
                  <dd>{net.appId}</dd>
                  <dt>Продукт / экземпляр</dt>
                  <dd>
                    {net.productSlug || "—"} / {net.instanceSlug || "—"}
                  </dd>
                  <dt>GUID</dt>
                  <dd>{net.instanceGuid || "—"}</dd>
                </dl>
              </section>
            )}
          </>
        )}

        {activeTab === "change" && (
          <section className="card admin-panel__card">
            <div className="row card-header-row">
              <h2>Все параметры</h2>
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
                Роль «хост» / «клиент» меняется автоматически при подключении на главном экране.
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

          </>
        )}
      </div>
    </div>
    </>
  );
}
