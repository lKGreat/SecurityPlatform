using Atlas.Application.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class AuthSessionRepository : IAuthSessionRepository
{
    private readonly ISqlSugarClient _db;

    public AuthSessionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task AddAsync(AuthSession session, CancellationToken cancellationToken)
    {
        return _db.Insertable(session).ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateAsync(AuthSession session, CancellationToken cancellationToken)
    {
        return _db.Updateable(session)
            .Where(x => x.Id == session.Id && x.TenantIdValue == session.TenantIdValue)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task RevokeAsync(TenantId tenantId, long sessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        return _db.Updateable<AuthSession>()
            .SetColumns(x => x.RevokedAt == revokedAt)
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == sessionId)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<AuthSession?> FindByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        return await _db.Queryable<AuthSession>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == id)
            .FirstAsync(cancellationToken);
    }
}
