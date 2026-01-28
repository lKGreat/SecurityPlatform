namespace Atlas.Application.Approval.Models;

/// <summary>
/// 发布审批流定义请求
/// </summary>
public record ApprovalFlowPublishRequest
{
    /// <summary>流程定义 ID</summary>
    public required long Id { get; init; }

    /// <summary>发布备注（可选）</summary>
    public string? Remark { get; init; }
}
