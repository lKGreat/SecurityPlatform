namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 审批流定义状态
/// </summary>
public enum ApprovalFlowStatus
{
    /// <summary>草稿</summary>
    Draft = 0,

    /// <summary>已发布</summary>
    Published = 1,

    /// <summary>已停用</summary>
    Disabled = 2
}
