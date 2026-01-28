namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 审批人分配策略类型
/// </summary>
public enum AssigneeType
{
    /// <summary>指定用户</summary>
    User = 0,

    /// <summary>按角色</summary>
    Role = 1,

    /// <summary>部门负责人</summary>
    DepartmentLeader = 2
}
