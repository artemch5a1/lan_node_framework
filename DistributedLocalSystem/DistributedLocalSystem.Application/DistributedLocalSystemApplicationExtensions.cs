using DistributedLocalSystem.Application.Net.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedLocalSystem.Application;

public static class DistributedLocalSystemApplicationExtensions
{
    public static IServiceCollection AddDistributedLocalSystemNetUseCases(
        this IServiceCollection services
    )
    {
        services.AddSingleton<IGetNetStatusUseCase, GetNetStatusUseCase>();
        services.AddSingleton<IGetNetRoleUseCase, GetNetRoleUseCase>();
        services.AddSingleton<IGetLanPeersUseCase, GetLanPeersUseCase>();
        services.AddSingleton<IGetNetConfigurationUseCase, GetNetConfigurationUseCase>();
        services.AddSingleton<IChangeNetConfigurationUseCase, ChangeNetConfigurationUseCase>();
        services.AddSingleton<IDisconnectFromRemoteHostUseCase, DisconnectFromRemoteHostUseCase>();
        return services;
    }
}
