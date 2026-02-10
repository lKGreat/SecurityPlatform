using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Repositories;

/// <summary>
/// 子流程关联记录仓储接口
/// </summary>
public interface IApprovalSubProcessLinkRepository
{
    Task AddAsync(ApprovalSubProcessLink entity, CancellationToken cancellationToken);

    Task UpdateAsync(ApprovalSubProcessLink entity, CancellationToken cancellationToken);

    Task<ApprovalSubProcessLink?> GetByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken);

    /// <summary>
    /// 根据子流程实例ID获取关联记录
    /// </summary>
    Task<ApprovalSubProcessLink?> GetByChildInstanceIdAsync(
        TenantId tenantId,
        long childInstanceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// 根据父流程实例ID获取所有子流程关联记录
    /// </summary>
    Task<IReadOnlyList<ApprovalSubProcessLink>> GetByParentInstanceIdAsync(
        TenantId tenantId,
        long parentInstanceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// 检查父流程是否有活跃的子流程
    /// </summary>
    Task<bool> HasActiveSubProcessAsync(
        TenantId tenantId,
        long parentInstanceId,
        CancellationToken cancellationToken);
}
