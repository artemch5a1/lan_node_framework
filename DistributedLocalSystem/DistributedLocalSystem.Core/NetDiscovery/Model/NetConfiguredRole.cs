using System.ComponentModel;

namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Режим экземпляра в LAN (хранится в конфигурации как строка API: host | client | none).</summary>
public enum NetConfiguredRole
{
    [Description("Выключен")]
    None = 0,

    [Description("Хост (вещание в LAN)")]
    Host = 1,

    [Description("Клиент (подключение к удалённому узлу)")]
    Client = 2,
}
