using Atlas.Application.Identity.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Identity.Abstractions;

public interface IPermissionCommandService
{
    Task<long> CreateAsync(TenantId tenantId, PermissionCreateRequest request, long id, CancellationToken cancellationToken);
    Task UpdateAsync(TenantId tenantId, long permissionId, PermissionUpdateRequest request, CancellationToken cancellationToken);
}
