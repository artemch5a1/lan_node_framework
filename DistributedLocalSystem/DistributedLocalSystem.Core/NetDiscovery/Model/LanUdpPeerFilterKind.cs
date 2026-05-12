namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>Как фильтровать входящие UDP beacon-сообщения по имени сервиса.</summary>
public enum LanUdpPeerFilterKind
{
    /// <summary>Только точное совпадение полной строки beacon (коллизия перед запуском host).</summary>
    ExactBeaconName,

    /// <summary>Тот же product slug (авто-поиск хоста для client по UDP).</summary>
    SameProductSlug,
}
