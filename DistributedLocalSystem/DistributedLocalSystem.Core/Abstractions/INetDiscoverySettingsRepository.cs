using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>Единственный источник настроек LAN discovery: SQLite + потокобезопасный снимок после чтения БД.</summary>
public interface INetDiscoverySettingsRepository
{
    /// <summary>Создаёт БД при необходимости, при пустой таблице — сид. Должен выполниться до <see cref="GetCurrent"/>.</summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>Последний снимок после <see cref="EnsureInitializedAsync"/> или <see cref="ReloadFromDatabaseAsync"/> (копия).</summary>
    DiscoveryOptions GetCurrent();

    /// <summary>Перечитывает строку из БД, обновляет снимок. Вызывать после правки БД или из API/UI.</summary>
    Task<DiscoveryOptions> ReloadFromDatabaseAsync(CancellationToken cancellationToken = default);

    Task<DiscoveryOptions> UpdateConfiguration(
        DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken = default
    );
}
