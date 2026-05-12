using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.NetDiscovery;
using DistributedLocalSystem.Core.Persistence;
using DistributedLocalSystem.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistributedLocalSystem.Infrastructure.Persistence.Repositories;

public sealed class NetDiscoverySettingsRepository : INetDiscoverySettingsRepository
{
    private readonly IDbContextFactory<DistributedLocalStorageContext> _factory;
    private readonly ILogger<NetDiscoverySettingsRepository> _log;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly object _snapshotGate = new();

    private DiscoveryOptions _snapshot = null!;
    private bool _initialized;

    public NetDiscoverySettingsRepository(
        IDbContextFactory<DistributedLocalStorageContext> factory,
        ILogger<NetDiscoverySettingsRepository> log
    )
    {
        _factory = factory;
        _log = log;
    }

    public async Task<DiscoveryOptions> UpdateConfiguration(
        DiscoveryOptions newDiscoveryOptions,
        CancellationToken cancellationToken = default
    )
    {
        DiscoveryOptions toPersist = NetDiscoverySettingsDefaults.Clone(newDiscoveryOptions);
        NetDiscoverySettingsDefaults.SyncComputedAppId(toPersist);
        NetDiscoverySettingsDefaults.ApplyRoleFromRemoteHost(toPersist);

        await using DistributedLocalStorageContext db = await _factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        int count = await db
            .NetDiscoverySettings.ExecuteUpdateAsync(
                x =>
                    x.SetProperty(i => i.AppId, toPersist.AppId)
                        .SetProperty(i => i.Role, toPersist.Role)
                        .SetProperty(i => i.ProductSlug, toPersist.ProductSlug)
                        .SetProperty(i => i.InstanceSlug, toPersist.InstanceSlug)
                        .SetProperty(i => i.InstanceGuid, toPersist.InstanceGuid)
                        .SetProperty(i => i.RemoteHostIp, toPersist.RemoteHostIp)
                        .SetProperty(i => i.BeaconIntervalMs, toPersist.BeaconIntervalMs)
                        .SetProperty(i => i.DiscoveryTimeoutMs, toPersist.DiscoveryTimeoutMs)
                        .SetProperty(i => i.LanPort, toPersist.LanPort)
                        .SetProperty(i => i.ProtocolVersion, toPersist.ProtocolVersion)
                        .SetProperty(i => i.UdpPort, toPersist.UdpPort),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (count < 1)
            throw new Exception("Ошибка обновления данных");

        return await LoadOrSeedAndPublishSnapshotAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            await LoadOrSeedAndPublishSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _initGate.Release();
        }
    }

    public DiscoveryOptions GetCurrent()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Net discovery settings are not loaded. Call EnsureInitializedAsync (or ReloadFromDatabaseAsync) first."
            );
        }

        lock (_snapshotGate)
        {
            return NetDiscoverySettingsDefaults.Clone(_snapshot);
        }
    }

    public async Task<DiscoveryOptions> ReloadFromDatabaseAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadOrSeedAndPublishSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _initGate.Release();
        }
    }

    private async Task<DiscoveryOptions> LoadOrSeedAndPublishSnapshotAsync(
        CancellationToken cancellationToken
    )
    {
        await using DistributedLocalStorageContext db = await _factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await NetDiscoverySqliteSchema
            .ApplyPendingColumnAddsAsync(db, cancellationToken)
            .ConfigureAwait(false);

        NetDiscoverySettingsEntity? row = await db
            .NetDiscoverySettings.FirstOrDefaultAsync(
                x => x.Id == NetDiscoverySettingsEntity.SingleRowId,
                cancellationToken
            )
            .ConfigureAwait(false);

        bool seeded = false;
        if (row is null)
        {
            NetDiscoverySettingsEntity seed = NetDiscoverySettingsDefaults.CreateSeedEntity();
            db.NetDiscoverySettings.Add(seed);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            row = seed;
            seeded = true;
        }
        else
        {
            NetDiscoveryRowNormalizer.Normalize(row);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        DiscoveryOptions mapped = NetDiscoverySettingsDefaults.ToDiscoveryOptions(row);

        lock (_snapshotGate)
        {
            _snapshot = NetDiscoverySettingsDefaults.Clone(mapped);
            _initialized = true;
        }

        if (seeded)
        {
            _log.LogInformation(
                "SQLite: seeded net_discovery_settings (AppId={AppId}, Role={Role})",
                row.AppId,
                row.Role
            );
        }

        return NetDiscoverySettingsDefaults.Clone(mapped);
    }
}
