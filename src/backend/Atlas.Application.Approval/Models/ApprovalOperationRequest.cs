using Atlas.Domain.Approval.Enums;

namespace Atlas.Application.Approval.Models;

/// <summary>
/// 审批流运行时操作请求
/// </summary>
public sealed class ApprovalOperationRequest
{
    /// <summary>操作类型</summary>
    public ApprovalOperationType OperationType { get; set; }

    /// <summary>操作说明/意见</summary>
    public string? Comment { get; set; }

    /// <summary>目标节点ID（用于退回任意节点）</summary>
    public string? TargetNodeId { get; set; }

    /// <summary>目标处理人值（用于转办、变更处理人）</summary>
    public string? TargetAssigneeValue { get; set; }

    /// <summary>额外审批人列表（用于加签）</summary>
    public List<string>? AdditionalAssigneeValues { get; set; }

    /// <summary>
    /// 加签类型（仅在 OperationType = AddAssignee 时有效）
    /// 0 = 并行加签（默认，在当前节点创建并行任务）
    /// 1 = 前加签（挂起当前任务，加签人先审，完成后恢复原任务）
    /// 2 = 后加签（当前任务正常审批，完成后流转到加签人）
    /// </summary>
    public int AddSignType { get; set; }

    /// <summary>幂等键（用于防止重复提交，由客户端生成）</summary>
    public string? IdempotencyKey { get; set; }
}
