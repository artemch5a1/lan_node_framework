using DistributedLocalSystem.Core.Persistence.Abstractions;

namespace DistributedLocalSystem.Core.NetDiscovery;

/// <summary>
/// Идентичность приложения в LAN UDP discovery: то же значение, что <c>serviceName</c> в UdpDiscovery.Net
/// (поле <see cref="DiscoveryOptions.AppId"/> из <see cref="INetDiscoverySettingsRepository.GetCurrent"/>).
/// </summary>
public sealed class DiscoveryServiceIdentity
{
    private readonly INetDiscoverySettingsRepository? _settings;
    private readonly string? _fixedExpectedServiceName;

    private DiscoveryServiceIdentity(INetDiscoverySettingsRepository settings) =>
        _settings = settings;

    private DiscoveryServiceIdentity(string fixedExpectedServiceName) =>
        _fixedExpectedServiceName = fixedExpectedServiceName;

    public static DiscoveryServiceIdentity FromRepository(
        INetDiscoverySettingsRepository settings
    ) => new(settings);

    /// <summary>Для тестов без репозитория.</summary>
    public static DiscoveryServiceIdentity FromConfiguredAppId(string? configuredAppId)
    {
        string normalized = (configuredAppId ?? string.Empty).Trim();
        return new DiscoveryServiceIdentity(normalized);
    }

    public string ExpectedServiceName =>
        _settings is not null
            ? (_settings.GetCurrent().AppId ?? string.Empty).Trim()
            : _fixedExpectedServiceName ?? string.Empty;

    /// <summary>Объявленное удалённым узлом имя совпадает с нашим (игнорируем чужие beacon’ы).</summary>
    public bool MatchesPeerAdvertisedName(string? peerServiceName) =>
        string.Equals(peerServiceName, ExpectedServiceName, StringComparison.Ordinal);
}
