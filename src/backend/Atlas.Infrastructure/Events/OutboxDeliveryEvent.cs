using Atlas.Core.Events;
using Atlas.Core.Tenancy;
using Atlas.Domain.Events;

namespace Atlas.Infrastructure.Events;

/// <summary>
/// Outbox 消息投递事件。
/// 由 OutboxProcessorHostedService 发布，承载原始的 Outbox 消息，
/// 各集成事件 handler 通过实现 IDomainEventHandler&lt;OutboxDeliveryEvent&gt; 来处理。
/// </summary>
public sealed record OutboxDeliveryEvent : IDomainEvent
{
    public OutboxDeliveryEvent(OutboxMessage message)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
        TenantId = new TenantId(message.TenantId);
        Message = message;
    }

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public TenantId TenantId { get; }

    /// <summary>原始 Outbox 消息</summary>
    public OutboxMessage Message { get; }
}
