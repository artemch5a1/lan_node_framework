namespace DistributedLocalSystem.Core.Discovery;

/// <summary>
/// Идентичность приложения в LAN UDP discovery: то же значение, что <c>serviceName</c> в UdpDiscovery.Net
/// (в конфигурации — <see cref="DiscoveryOptions.AppId"/>).
/// </summary>
public sealed class DiscoveryServiceIdentity
{
    private DiscoveryServiceIdentity(string expectedServiceName)
    {
        ExpectedServiceName = expectedServiceName;
    }

    public string ExpectedServiceName { get; }

    /// <param name="configuredAppId">Значение <see cref="DiscoveryOptions.AppId"/> из конфигурации.</param>
    public static DiscoveryServiceIdentity FromConfiguredAppId(string? configuredAppId)
    {
        string normalized = (configuredAppId ?? string.Empty).Trim();
        return new DiscoveryServiceIdentity(normalized);
    }

    /// <summary>Объявленное удалённым узлом имя совпадает с нашим (игнорируем чужие beacon’ы).</summary>
    public bool MatchesPeerAdvertisedName(string? peerServiceName) =>
        string.Equals(peerServiceName, ExpectedServiceName, StringComparison.Ordinal);
}
