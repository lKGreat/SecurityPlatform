using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Repositories;

/// <summary>
/// 审批定时任务仓储接口
/// </summary>
public interface IApprovalTimerJobRepository
{
    Task AddAsync(ApprovalTimerJob entity, CancellationToken cancellationToken);

    Task UpdateAsync(ApprovalTimerJob entity, CancellationToken cancellationToken);

    Task<ApprovalTimerJob?> GetByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取所有已到期但未执行的定时任务
    /// </summary>
    Task<IReadOnlyList<ApprovalTimerJob>> GetPendingDueJobsAsync(
        TenantId tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// 根据实例和节点获取定时任务
    /// </summary>
    Task<ApprovalTimerJob?> GetByInstanceAndNodeAsync(
        TenantId tenantId,
        long instanceId,
        string nodeId,
        CancellationToken cancellationToken);
}
