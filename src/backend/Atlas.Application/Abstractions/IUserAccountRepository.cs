using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<UserAccount?> FindByUsernameAsync(TenantId tenantId, string username, CancellationToken cancellationToken);
    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
    Task UpdateAsync(UserAccount account, CancellationToken cancellationToken);
}
