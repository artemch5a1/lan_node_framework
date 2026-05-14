using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.NetDiscovery.Identity;

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

    /// <summary>Полное совпадение beacon-строки (коллизия хоста, legacy single AppId).</summary>
    public bool MatchesPeerExactBeacon(string? peerServiceName) =>
        string.Equals(peerServiceName, ExpectedServiceName, StringComparison.Ordinal);

    /// <summary>
    /// Тот же product slug в формате <see cref="LanBeaconName"/>; для legacy-имени без DLS — точное совпадение с <see cref="ExpectedServiceName"/>.
    /// </summary>
    public bool MatchesPeerSameProduct(string? peerServiceName)
    {
        if (!LanBeaconName.TryParse(ExpectedServiceName, out LanBeaconParsed local))
            return MatchesPeerExactBeacon(peerServiceName);

        if (!LanBeaconName.TryParse(peerServiceName, out LanBeaconParsed remote))
            return false;

        return string.Equals(local.ProductSlug, remote.ProductSlug, StringComparison.Ordinal);
    }
}
