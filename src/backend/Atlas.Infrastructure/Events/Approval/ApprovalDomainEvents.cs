using Atlas.Core.Events;
using Atlas.Core.Tenancy;

namespace Atlas.Infrastructure.Events.Approval;

/// <summary>
/// 审批流实例状态变更的通用领域事件。
/// 将审批模块的领域概念暴露给通用事件总线，使其他有界上下文也能订阅。
/// </summary>
public sealed record ApprovalInstanceDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public TenantId TenantId { get; init; }

    /// <summary>实例ID</summary>
    public long InstanceId { get; init; }

    /// <summary>流程定义ID</summary>
    public long DefinitionId { get; init; }

    /// <summary>业务主键（如 tableKey:recordId）</summary>
    public string BusinessKey { get; init; } = string.Empty;

    /// <summary>携带的业务数据快照（JSON）</summary>
    public string? DataJson { get; init; }

    /// <summary>触发人用户ID</summary>
    public long ActorUserId { get; init; }

    /// <summary>事件类型</summary>
    public ApprovalInstanceEventType EventType { get; init; }
}

/// <summary>审批任务事件（审批/驳回）</summary>
public sealed record ApprovalTaskDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public TenantId TenantId { get; init; }

    public long InstanceId { get; init; }
    public long TaskId { get; init; }
    public string NodeId { get; init; } = string.Empty;
    public string BusinessKey { get; init; } = string.Empty;
    public long ActorUserId { get; init; }
    public string? Comment { get; init; }
    public ApprovalTaskEventType EventType { get; init; }
}

public enum ApprovalInstanceEventType
{
    Started,
    Completed,
    Rejected,
    Canceled
}

public enum ApprovalTaskEventType
{
    Approved,
    Rejected
}
