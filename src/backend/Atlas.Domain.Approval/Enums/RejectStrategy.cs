namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 驳回策略
/// </summary>
public enum RejectStrategy
{
    /// <summary>退回上一步</summary>
    ToPrevious = 1,

    /// <summary>退回发起人</summary>
    ToInitiator = 2,

    /// <summary>退回任意节点</summary>
    ToAnyNode = 3,

    /// <summary>终止审批流程</summary>
    TerminateApproval = 4,

    /// <summary>退回模型父节点（上一个审批节点）</summary>
    ToParentNode = 5
}
