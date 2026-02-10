using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Repositories;

/// <summary>
/// 审批触发器任务仓储接口
/// </summary>
public interface IApprovalTriggerJobRepository
{
    Task AddAsync(ApprovalTriggerJob entity, CancellationToken cancellationToken);

    Task UpdateAsync(ApprovalTriggerJob entity, CancellationToken cancellationToken);

    Task<ApprovalTriggerJob?> GetByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取所有已到期但未执行的触发器任务
    /// </summary>
    Task<IReadOnlyList<ApprovalTriggerJob>> GetPendingDueJobsAsync(
        TenantId tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// 根据实例和节点获取触发器任务
    /// </summary>
    Task<ApprovalTriggerJob?> GetByInstanceAndNodeAsync(
        TenantId tenantId,
        long instanceId,
        string nodeId,
        CancellationToken cancellationToken);
}
