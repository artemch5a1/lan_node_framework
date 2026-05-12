using System.Data;
using Microsoft.EntityFrameworkCore;

namespace DistributedLocalSystem.Core.Persistence;

/// <summary>
/// Добавляет колонки к существующей SQLite-таблице (проект использует <see cref="DatabaseFacade.EnsureCreatedAsync"/>, без EF Migrations).
/// </summary>
internal static class NetDiscoverySqliteSchema
{
    internal static async Task ApplyPendingColumnAddsAsync(
        DistributedLocalStorageContext db,
        CancellationToken cancellationToken
    )
    {
        System.Data.Common.DbConnection conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        HashSet<string> cols = new(StringComparer.OrdinalIgnoreCase);
        await using (System.Data.Common.DbCommand pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(net_discovery_settings)";
            await using System.Data.Common.DbDataReader reader = await pragma
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                cols.Add(reader.GetString(1));
        }

        async Task AddColumnIfMissingAsync(string columnName, string sqlTypeAndConstraints)
        {
            if (cols.Contains(columnName))
                return;

            await using System.Data.Common.DbCommand cmd = conn.CreateCommand();
            cmd.CommandText =
                $"ALTER TABLE net_discovery_settings ADD COLUMN {columnName} {sqlTypeAndConstraints}";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            cols.Add(columnName);
        }

        await AddColumnIfMissingAsync("ProductSlug", "TEXT NOT NULL DEFAULT ''").ConfigureAwait(false);
        await AddColumnIfMissingAsync("InstanceSlug", "TEXT NOT NULL DEFAULT ''").ConfigureAwait(false);
        await AddColumnIfMissingAsync("InstanceGuid", "TEXT NOT NULL DEFAULT ''").ConfigureAwait(false);
        await AddColumnIfMissingAsync("RemoteHostIp", "TEXT NULL").ConfigureAwait(false);
    }
}
