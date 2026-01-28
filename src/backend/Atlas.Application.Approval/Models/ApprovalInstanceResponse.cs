using Atlas.Domain.Approval.Enums;

namespace Atlas.Application.Approval.Models;

/// <summary>
/// 审批流程实例响应
/// </summary>
public record ApprovalInstanceResponse
{
    /// <summary>实例 ID</summary>
    public required long Id { get; init; }

    /// <summary>定义 ID</summary>
    public required long DefinitionId { get; init; }

    /// <summary>业务 key</summary>
    public required string BusinessKey { get; init; }

    /// <summary>发起人 ID</summary>
    public required long InitiatorUserId { get; init; }

    /// <summary>业务数据 JSON</summary>
    public string? DataJson { get; init; }

    /// <summary>实例状态</summary>
    public required ApprovalInstanceStatus Status { get; init; }

    /// <summary>启动时间</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>结束时间</summary>
    public DateTimeOffset? EndedAt { get; init; }
}
