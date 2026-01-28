namespace Atlas.Application.Approval.Models;

/// <summary>
/// 审批任务决策请求（同意或驳回）
/// </summary>
public record ApprovalTaskDecideRequest
{
    /// <summary>任务 ID</summary>
    public required long TaskId { get; init; }

    /// <summary>是否同意（true=同意, false=驳回）</summary>
    public required bool Approved { get; init; }

    /// <summary>审批意见</summary>
    public string? Comment { get; init; }
}
