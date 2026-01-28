using Atlas.Domain.Approval.Enums;

namespace Atlas.Application.Approval.Models;

/// <summary>
/// 审批流定义响应
/// </summary>
public record ApprovalFlowDefinitionResponse
{
    /// <summary>流程定义 ID</summary>
    public required long Id { get; init; }

    /// <summary>流程名称</summary>
    public required string Name { get; init; }

    /// <summary>流程定义 JSON</summary>
    public required string DefinitionJson { get; init; }

    /// <summary>版本号</summary>
    public required int Version { get; init; }

    /// <summary>状态</summary>
    public required ApprovalFlowStatus Status { get; init; }

    /// <summary>发布时间</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>发布人 ID</summary>
    public long? PublishedByUserId { get; init; }
}
