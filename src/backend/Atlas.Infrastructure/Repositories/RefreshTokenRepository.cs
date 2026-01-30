using Atlas.Application.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ISqlSugarClient _db;

    public RefreshTokenRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        return _db.Insertable(token).ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        return _db.Updateable(token)
            .Where(x => x.Id == token.Id && x.TenantIdValue == token.TenantIdValue)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task RevokeBySessionAsync(TenantId tenantId, long sessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        return _db.Updateable<RefreshToken>()
            .SetColumns(x => x.RevokedAt == revokedAt)
            .Where(x => x.TenantIdValue == tenantId.Value && x.SessionId == sessionId)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<RefreshToken?> FindByHashAsync(TenantId tenantId, string tokenHash, CancellationToken cancellationToken)
    {
        return await _db.Queryable<RefreshToken>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.TokenHash == tokenHash)
            .FirstAsync(cancellationToken);
    }
}
