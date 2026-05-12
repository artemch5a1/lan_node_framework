using DistributedLocalSystem.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedLocalSystem.Infrastructure.Persistence;

public sealed class DistributedLocalStorageContext : DbContext
{
    public DistributedLocalStorageContext(DbContextOptions<DistributedLocalStorageContext> options)
        : base(options) { }

    public DbSet<NetDiscoverySettingsEntity> NetDiscoverySettings =>
        Set<NetDiscoverySettingsEntity>();
}
