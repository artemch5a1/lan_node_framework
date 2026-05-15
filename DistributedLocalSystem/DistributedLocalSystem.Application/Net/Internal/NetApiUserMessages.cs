namespace DistributedLocalSystem.Application.Net.Internal;

/// <summary>Краткие сообщения для клиента (рус.), без технических деталей исключений.</summary>
internal static class NetApiUserMessages
{
    public const string Unexpected =
        "Не удалось получить данные о сети. Обновите страницу или попробуйте позже.";

    public const string LanPeerScanFailed =
        "Не удалось выполнить сканирование узлов в LAN. Проверьте настройки и попробуйте снова.";

    public const string ConfigurationSaveFailed =
        "Не удалось сохранить настройки сети. Проверьте поля и попробуйте ещё раз.";

    public const string ConfigurationReloadAfterDisconnectFailed =
        "Не удалось применить настройки после отключения от узла.";

    /// <summary>Общий случай конфликта режима хоста (не «другой хост в LAN»).</summary>
    public const string HostRoleConflict =
        "Текущая конфигурация не позволяет включить режим хоста в этой сети.";
}
