using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Application.Abstractions;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session, CancellationToken cancellationToken);
    Task<AuthSession?> FindByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken);
    Task UpdateAsync(AuthSession session, CancellationToken cancellationToken);
    Task RevokeAsync(TenantId tenantId, long sessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}
