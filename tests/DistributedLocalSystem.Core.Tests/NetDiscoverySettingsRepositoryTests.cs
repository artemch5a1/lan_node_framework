using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence;
using DistributedLocalSystem.Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DistributedLocalSystem.Core.Tests;

public class NetDiscoverySettingsRepositoryTests
{
    [Fact]
    public async Task EnsureInitializedAsync_SeedsDefaultsThenReloadReadsSame()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-dltest.db");
        DbContextOptions<DistributedLocalStorageContext> options =
            new DbContextOptionsBuilder<DistributedLocalStorageContext>()
                .UseSqlite($"Data Source={path}")
                .Options;

        IDbContextFactory<DistributedLocalStorageContext> factory = new TestDbContextFactory(
            options
        );

        try
        {
            NetDiscoverySettingsRepository repo = new(
                factory,
                NullLogger<NetDiscoverySettingsRepository>.Instance
            );

            await repo.EnsureInitializedAsync();
            DiscoveryOptions first = repo.GetCurrent();
            Assert.Equal("op1-26", first.AppId);
            Assert.Equal(49152, first.UdpPort);

            DiscoveryOptions second = await repo.ReloadFromDatabaseAsync();
            Assert.Equal(first.AppId, second.AppId);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // best-effort
            }
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<DistributedLocalStorageContext>
    {
        private readonly DbContextOptions<DistributedLocalStorageContext> _options;

        public TestDbContextFactory(DbContextOptions<DistributedLocalStorageContext> options) =>
            _options = options;

        public DistributedLocalStorageContext CreateDbContext() => new(_options);
    }
}
