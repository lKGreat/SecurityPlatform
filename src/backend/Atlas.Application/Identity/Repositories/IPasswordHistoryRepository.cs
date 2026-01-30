using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Application.Identity.Repositories;

public interface IPasswordHistoryRepository
{
    Task<IReadOnlyList<PasswordHistory>> GetRecentAsync(
        TenantId tenantId,
        long userId,
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(PasswordHistory history, CancellationToken cancellationToken);

    Task DeleteExceptRecentAsync(
        TenantId tenantId,
        long userId,
        int keep,
        CancellationToken cancellationToken);
}
