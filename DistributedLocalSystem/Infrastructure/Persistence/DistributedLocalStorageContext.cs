using DistributedLocalSystem.Core.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedLocalSystem.Core.Persistence;

public sealed class DistributedLocalStorageContext : DbContext
{
    public DistributedLocalStorageContext(DbContextOptions<DistributedLocalStorageContext> options)
        : base(options) { }

    public DbSet<NetDiscoverySettingsEntity> NetDiscoverySettings =>
        Set<NetDiscoverySettingsEntity>();
}
