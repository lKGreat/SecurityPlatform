using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Repositories;

/// <summary>
/// 审批流程变量仓储接口
/// 当前能力边界：仅提供流程变量持久化读写接口；条件规则评估不在本接口职责范围。
/// 跟踪任务：APRV-128（https://tracker.local/APRV-128），预计版本：v1.4。
/// </summary>
public interface IApprovalProcessVariableRepository
{
    Task AddAsync(ApprovalProcessVariable entity, CancellationToken cancellationToken);

    Task UpdateAsync(ApprovalProcessVariable entity, CancellationToken cancellationToken);

    Task<ApprovalProcessVariable?> GetByInstanceAndNameAsync(
        TenantId tenantId,
        long instanceId,
        string variableName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovalProcessVariable>> GetByInstanceAsync(
        TenantId tenantId,
        long instanceId,
        CancellationToken cancellationToken);

    Task DeleteByInstanceAsync(
        TenantId tenantId,
        long instanceId,
        CancellationToken cancellationToken);
}
