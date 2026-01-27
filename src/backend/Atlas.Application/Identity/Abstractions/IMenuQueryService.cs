using Atlas.Application.Identity.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Identity.Abstractions;

public interface IMenuQueryService
{
    Task<PagedResult<MenuListItem>> QueryMenusAsync(
        PagedRequest request,
        TenantId tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuListItem>> QueryAllAsync(
        TenantId tenantId,
        CancellationToken cancellationToken);
}
