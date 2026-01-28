using Atlas.Domain.Approval.Enums;

namespace Atlas.Application.Approval.Models;

/// <summary>
/// 审批流定义列表项
/// </summary>
public record ApprovalFlowDefinitionListItem
{
    /// <summary>流程定义 ID</summary>
    public required long Id { get; init; }

    /// <summary>流程名称</summary>
    public required string Name { get; init; }

    /// <summary>版本号</summary>
    public required int Version { get; init; }

    /// <summary>状态</summary>
    public required ApprovalFlowStatus Status { get; init; }

    /// <summary>发布时间</summary>
    public DateTimeOffset? PublishedAt { get; init; }
}
