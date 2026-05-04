namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>Перечитывает настройки из БД и перестраивает host/client discovery.</summary>
public interface INetDiscoveryConfigurationReloadCoordinator
{
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
