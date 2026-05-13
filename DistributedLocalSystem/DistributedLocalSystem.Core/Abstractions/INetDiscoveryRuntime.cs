using System.Diagnostics.CodeAnalysis;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>
/// Среда выполнения LAN discovery (режимы host/client, снимки, смена конфигурации).
/// Реализация — инфраструктура; оркестрация сценариев — слой Application.
/// </summary>
public interface INetDiscoveryRuntime : IDisposable
{
    void RealignWithCurrentConfiguration();

    Task<DiscoveryOptions> ChangeConfiguration(
        DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken = default
    );

    DiscoveryOptions GetCurrentConfiguration();

    NetStatusDto GetStatus();

    void StartHost();

    void StartClient();

    void Stop();

    void RestartClientDiscoveryAfterRemoteHostFailure();

    bool TryGetHostProxyBaseUrl([NotNullWhen(true)] out string? baseUrl);
}
