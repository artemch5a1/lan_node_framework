using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DistributedLocalSystem.Infrastructure.Persistence;

/// <summary>
/// Временный SQLite для <c>dotnet ef migrations</c> (путь не используется в рантайме).
/// </summary>
public sealed class DistributedLocalStorageContextFactory
    : IDesignTimeDbContextFactory<DistributedLocalStorageContext>
{
    public DistributedLocalStorageContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<DistributedLocalStorageContext> builder = new();
        builder.UseSqlite("Data Source=ef-design-distributed-local.db");
        return new DistributedLocalStorageContext(builder.Options);
    }
}
