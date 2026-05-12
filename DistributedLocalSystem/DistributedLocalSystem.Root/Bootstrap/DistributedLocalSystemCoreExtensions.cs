using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery.Identity;
using DistributedLocalSystem.Infrastructure.Middleware;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Configuration;
using DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;
using DistributedLocalSystem.Infrastructure.Persistence;
using DistributedLocalSystem.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedLocalSystem.Root.Bootstrap;

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
        services.AddSingleton<ILanPeerScanService, LanPeerScanService>();
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
