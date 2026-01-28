using Atlas.Application.Approval.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Approval.Abstractions;

/// <summary>
/// 部门负责人管理服务接口
/// </summary>
public interface IApprovalDepartmentLeaderService
{
    Task SetLeaderAsync(
        TenantId tenantId,
        ApprovalDepartmentLeaderRequest request,
        CancellationToken cancellationToken);

    Task<long?> GetLeaderUserIdAsync(
        TenantId tenantId,
        long departmentId,
        CancellationToken cancellationToken);

    Task RemoveLeaderAsync(
        TenantId tenantId,
        long departmentId,
        CancellationToken cancellationToken);
}
