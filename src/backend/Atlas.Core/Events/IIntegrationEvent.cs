using Atlas.Core.Events;
using Atlas.Core.Tenancy;

namespace Atlas.Core.Events;

/// <summary>
/// 集成事件接口（跨有界上下文/跨系统边界的事件）。
/// 继承自 <see cref="IDomainEvent"/>，增加序列化能力和事件类型标识，
/// 可被 Outbox 持久化后异步投递。
/// </summary>
public interface IIntegrationEvent : IDomainEvent
{
    /// <summary>
    /// 事件类型标识（如 "approval.instance.completed"）。
    /// 用于路由、过滤和 Outbox 反序列化。
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// 事件 Payload 的 JSON 序列化（用于 Outbox 持久化和 Webhook 投递）。
    /// </summary>
    string ToPayload();
}
