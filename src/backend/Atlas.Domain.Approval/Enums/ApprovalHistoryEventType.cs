namespace Atlas.Domain.Approval.Enums;

/// <summary>
/// 审批历史事件类型
/// </summary>
public enum ApprovalHistoryEventType
{
    /// <summary>流程启动</summary>
    InstanceStarted = 0,

    /// <summary>任务创建</summary>
    TaskCreated = 1,

    /// <summary>任务同意</summary>
    TaskApproved = 2,

    /// <summary>任务驳回</summary>
    TaskRejected = 3,

    /// <summary>节点推进</summary>
    NodeAdvanced = 4,

    /// <summary>流程完成</summary>
    InstanceCompleted = 5,

    /// <summary>流程驳回</summary>
    InstanceRejected = 6,

    /// <summary>流程取消</summary>
    InstanceCanceled = 7
}
