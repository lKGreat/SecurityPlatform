using Atlas.Core.Tenancy;

namespace Atlas.Core.Events;

/// <summary>
/// 领域事件基础接口。
/// 进程内发布/订阅，用于有界上下文内部解耦，不跨进程。
/// </summary>
public interface IDomainEvent
{
    /// <summary>事件唯一标识</summary>
    Guid EventId { get; }

    /// <summary>事件发生时间</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>所属租户（Empty 表示系统级事件）</summary>
    TenantId TenantId { get; }
}
