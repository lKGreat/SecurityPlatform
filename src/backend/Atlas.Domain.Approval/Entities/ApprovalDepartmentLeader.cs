using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Approval.Entities;

/// <summary>
/// 审批流模块内的部门负责人映射（支持"部门负责人"审批人策略）
/// </summary>
public sealed class ApprovalDepartmentLeader : TenantEntity
{
    public ApprovalDepartmentLeader()
        : base(TenantId.Empty)
    {
    }

    public ApprovalDepartmentLeader(TenantId tenantId, long departmentId, long leaderUserId, long id)
        : base(tenantId)
    {
        Id = id;
        DepartmentId = departmentId;
        LeaderUserId = leaderUserId;
    }

    /// <summary>部门 ID</summary>
    public long DepartmentId { get; private set; }

    /// <summary>部门负责人用户 ID</summary>
    public long LeaderUserId { get; private set; }

    public void UpdateLeader(long leaderUserId)
    {
        LeaderUserId = leaderUserId;
    }
}
