using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Repositories;

/// <summary>
/// 部门负责人映射仓储接口
/// </summary>
public interface IApprovalDepartmentLeaderRepository
{
    Task AddAsync(ApprovalDepartmentLeader entity, CancellationToken cancellationToken);

    Task UpdateAsync(ApprovalDepartmentLeader entity, CancellationToken cancellationToken);

    Task<ApprovalDepartmentLeader?> GetByDepartmentIdAsync(
        TenantId tenantId,
        long departmentId,
        CancellationToken cancellationToken);

    Task<long?> GetLeaderUserIdAsync(TenantId tenantId, long departmentId, CancellationToken cancellationToken);

    Task DeleteByDepartmentIdAsync(TenantId tenantId, long departmentId, CancellationToken cancellationToken);
}
