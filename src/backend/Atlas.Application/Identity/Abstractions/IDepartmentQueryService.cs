using Atlas.Application.Identity.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Identity.Abstractions;

public interface IDepartmentQueryService
{
    Task<PagedResult<DepartmentListItem>> QueryDepartmentsAsync(
        PagedRequest request,
        TenantId tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentListItem>> QueryAllAsync(
        TenantId tenantId,
        CancellationToken cancellationToken);
}
