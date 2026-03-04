using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 审批运行时操作处理归属说明（Handler / CommandService / 控制器）
/// </summary>
public static class ApprovalOperationHandlingRegistry
{
    /// <summary>
    /// 获取某个操作类型的处理归属说明，便于统一排障与文档对齐
    /// </summary>
    public static string GetResponsibleLayer(ApprovalOperationType operationType)
    {
        return operationType switch
        {
            ApprovalOperationType.AddAssignee => "审批操作分发器：AddAssigneeOperationHandler",
            ApprovalOperationType.AddFutureAssignee => "审批操作分发器：AddFutureAssigneeOperationHandler",
            ApprovalOperationType.Append => "审批操作分发器：AppendAssigneeHandler",
            ApprovalOperationType.BackToAnyNode => "审批操作分发器：BackToAnyNodeOperationHandler",
            ApprovalOperationType.BackToModify => "审批操作分发器：BackToModifyOperationHandler",
            ApprovalOperationType.BackToPrevModify => "未在 dispatcher 内显式处理，请确认是否需要独立 Handler 或由专用接口实现",
            ApprovalOperationType.BatchTransfer => "审批操作分发器：BatchTransferHandler",
            ApprovalOperationType.ChangeAssignee => "审批操作分发器：ChangeAssigneeOperationHandler",
            ApprovalOperationType.ChangeFutureAssignee => "审批操作分发器：ChangeFutureAssigneeOperationHandler",
            ApprovalOperationType.Claim => "审批操作分发器：ClaimTaskHandler",
            ApprovalOperationType.Communicate => "审批操作分发器：CommunicateHandler",
            ApprovalOperationType.DrawBackAgree => "审批操作分发器：DrawBackAgreeOperationHandler",
            ApprovalOperationType.Forward => "审批操作分发器：ForwardOperationHandler",
            ApprovalOperationType.Jump => "审批操作分发器：JumpTaskHandler",
            ApprovalOperationType.ProcessMoveAhead => "审批操作分发器：ProcessMoveAheadOperationHandler",
            ApprovalOperationType.ProcessDrawBack => "审批操作分发器：ProcessDrawBackOperationHandler",
            ApprovalOperationType.RecoverToHistory => "审批操作分发器：RecoverToHistoryOperationHandler",
            ApprovalOperationType.Release => "审批操作分发器：ReleaseTaskHandler",
            ApprovalOperationType.RemoveAssignee => "审批操作分发器：RemoveAssigneeOperationHandler",
            ApprovalOperationType.RemoveFutureAssignee => "审批操作分发器：RemoveFutureAssigneeOperationHandler",
            ApprovalOperationType.Reclaim => "审批操作分发器：ReclaimTaskHandler",
            ApprovalOperationType.Resume => "审批操作分发器：ResumeTaskHandler",
            ApprovalOperationType.SaveDraft => "审批操作分发器：SaveDraftOperationHandler",
            ApprovalOperationType.Transfer => "审批操作分发器：TransferOperationHandler",
            ApprovalOperationType.Undertake => "审批操作分发器：UndertakeOperationHandler",
            ApprovalOperationType.Urge => "审批操作分发器：UrgeTaskHandler",
            ApprovalOperationType.Terminate => "控制器级运行时接口：/instances/{id}/termination",
            ApprovalOperationType.Stop => "控制器级接口：ApprovalRuntimeController.Termination/Cancel 的终止语义待与 Stop 映射统一确认",
            ApprovalOperationType.Agree => "任务接口：ApprovalTasksController.DecideAsync -> IApprovalRuntimeCommandService.ApproveTaskAsync",
            ApprovalOperationType.Disagree => "任务接口：ApprovalTasksController.DecideAsync -> IApprovalRuntimeCommandService.RejectTaskAsync",
            ApprovalOperationType.Preview => "控制器级UI操作：ApprovalRuntimeController.PreviewInstanceAsync",
            ApprovalOperationType.Print => "控制器级UI操作：ApprovalRuntimeController.PrintInstanceAsync",
            ApprovalOperationType.Submit => "启动接口：ApprovalRuntimeController.StartAsync -> IApprovalRuntimeCommandService.StartAsync",
            ApprovalOperationType.Resubmit => "草稿提交接口：ApprovalRuntimeController.SubmitDraftAsync -> IApprovalRuntimeCommandService.SubmitDraftAsync",
            ApprovalOperationType.Abandon => "控制器级接口：ApprovalRuntimeController.CancelAsync（语义待与枚举 Stop/Abandon 映射确认）",
            ApprovalOperationType.Delegate => "任务接口：ApprovalTasksController.DelegateAsync (delegation) 与 ResolveAsync 为成对返回流程",
            ApprovalOperationType.ChooseAssignee => "未在当前版本中显式映射到 handler/controller（需补齐）",
            ApprovalOperationType.ViewBusinessProcess => "未在当前版本中显式映射到 handler/controller（需补齐）",

            _ => "未明确映射，请补齐处理归属"
        };
    }
}
