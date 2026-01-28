namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 审批流实例状态
/// </summary>
public enum ApprovalInstanceStatus
{
    /// <summary>运行中</summary>
    Running = 0,

    /// <summary>已完成</summary>
    Completed = 1,

    /// <summary>已驳回</summary>
    Rejected = 2,

    /// <summary>已取消</summary>
    Canceled = 3
}
