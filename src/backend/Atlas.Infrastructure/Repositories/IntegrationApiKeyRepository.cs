using Atlas.Application.Integration;
using Atlas.Core.Tenancy;
using Atlas.Domain.Integration;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class IntegrationApiKeyRepository : IIntegrationApiKeyRepository
{
    private readonly ISqlSugarClient _db;

    public IntegrationApiKeyRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<IntegrationApiKey>> GetActiveByTenantAsync(
        TenantId tenantId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.Queryable<IntegrationApiKey>()
            .Where(k => k.TenantIdValue == tenantId.Value
                        && k.IsActive
                        && (k.ExpiresAt == null || k.ExpiresAt > now))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLastUsedAtAsync(long id, DateTimeOffset usedAt, CancellationToken cancellationToken)
    {
        await _db.Updateable<IntegrationApiKey>()
            .SetColumns(k => k.LastUsedAt == usedAt)
            .Where(k => k.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }
}
