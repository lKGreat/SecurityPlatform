namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 审批人与提交人为同一人时的处理策略
/// </summary>
public enum NodeApproveSelf
{
    /// <summary>由发起人对自己审批（默认，不做特殊处理）</summary>
    InitiatorThemselves = 0,

    /// <summary>自动跳过</summary>
    AutoSkip = 1,

    /// <summary>转交给直接上级审批</summary>
    TransferDirectSuperior = 2,

    /// <summary>转交给部门负责人审批</summary>
    TransferDepartmentHead = 3
}
