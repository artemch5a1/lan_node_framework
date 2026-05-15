using DistributedLocalSystem.Application.Net.Configuration;
using DistributedLocalSystem.Application.Net.Connection;
using DistributedLocalSystem.Application.Net.Discovery;
using DistributedLocalSystem.Application.Net.Hosting;
using DistributedLocalSystem.Application.Net.Orchestration;
using DistributedLocalSystem.Application.Net.Remote;
using DistributedLocalSystem.Application.Net.Status;
using DistributedLocalSystem.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedLocalSystem.Application.Net.DependencyInjection;

/// <summary>Регистрация сервисов LAN Application-слоя.</summary>
public static class NetApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedLocalSystemApplicationNet(
        this IServiceCollection services
    )
    {
        services.AddSingleton<NetRuntimeSnapshotReader>();
        services.AddSingleton<NetConfigurationStateReader>();
        services.AddSingleton<LanNodeListService>();
        services.AddSingleton<NetConfigurationPersistenceService>();
        services.AddSingleton<NetRemoteHostPreConnectValidator>();
        services.AddSingleton<NetConfigurationApplyService>();
        services.AddSingleton<NetConnectByIpService>();
        services.AddSingleton<NetDisconnectFromRemoteService>();
        services.AddSingleton<INetLanOrchestrator, NetLanOrchestrator>();
        services.AddHostedService<NetDiscoveryHostedService>();
        return services;
    }
}
