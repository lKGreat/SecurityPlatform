using Atlas.Application.Identity.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Identity.Abstractions;

public interface IUserQueryService
{
    Task<PagedResult<UserListItem>> QueryUsersAsync(
        PagedRequest request,
        TenantId tenantId,
        CancellationToken cancellationToken);

    Task<UserDetail?> GetDetailAsync(
        long id,
        TenantId tenantId,
        CancellationToken cancellationToken);
}
