using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DistributedLocalSystem.Core.Middleware;
using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence;
using DistributedLocalSystem.Core.Persistence.Abstractions;
using DistributedLocalSystem.Core.Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedLocalSystem.Core;

public static class DistributedLocalSystemCoreExtensions
{
    public static IServiceCollection AddDistributedLocalSystemCore(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContextFactory<DistributedLocalStorageContext>(
            (_, options) =>
            {
                string dataSource = ResolveSqliteDataSource(configuration);
                options.UseSqlite($"Data Source={dataSource}");
            }
        );

        services.AddSingleton<NetDiscoverySettingsRepository>();
        services.AddSingleton<INetDiscoverySettingsRepository>(static sp =>
            sp.GetRequiredService<NetDiscoverySettingsRepository>()
        );

        services.AddSingleton(static sp =>
            DiscoveryServiceIdentity.FromRepository(
                sp.GetRequiredService<INetDiscoverySettingsRepository>()
            )
        );

        services.AddSingleton<NetDiscoveryService>();
        services.AddSingleton<
            INetDiscoveryConfigurationReloadCoordinator,
            NetDiscoveryConfigurationReloadCoordinator
        >();
        services.AddHostedService<NetDiscoveryHostedService>();

        services
            .AddHttpClient("hostProxy")
            .ConfigureHttpClient(static c => c.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(static () =>
                new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.None,
                }
            );

        services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        });

        return services;
    }

    public static IApplicationBuilder UseDistributedLocalSystemCoreProxy(
        this IApplicationBuilder app
    )
    {
        return app.UseMiddleware<ClientHostProxyMiddleware>();
    }

    private static string ResolveSqliteDataSource(IConfiguration configuration)
    {
        string? raw = configuration
            .GetSection(DistributedLocalPersistenceOptions.SectionName)
            .Get<DistributedLocalPersistenceOptions>()
            ?.DataSource;

        if (string.IsNullOrWhiteSpace(raw))
            return Path.Combine(AppContext.BaseDirectory, "distributed-local.db");

        string trimmed = raw.Trim();
        return Path.IsPathRooted(trimmed)
            ? trimmed
            : Path.Combine(AppContext.BaseDirectory, trimmed);
    }
}
