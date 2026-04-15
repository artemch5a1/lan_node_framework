using Backend.Application.Abstractions;
using Backend.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IBookRepository, InMemoryBookRepository>();
        return services;
    }
}
