using System.Text.Json;
using Atlas.Application.Events;
using Atlas.Core.Abstractions;
using Atlas.Core.Events;
using Atlas.Domain.Events;

namespace Atlas.Infrastructure.Events;

/// <summary>
/// Outbox 发布器，将集成事件持久化到 Outbox 表，由后台服务异步投递。
/// 确保"至少一次"投递语义。
/// </summary>
public sealed class OutboxPublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IIdGeneratorAccessor _idGen;

    public OutboxPublisher(IOutboxRepository outboxRepository, IIdGeneratorAccessor idGen)
    {
        _outboxRepository = outboxRepository;
        _idGen = idGen;
    }

    /// <summary>
    /// 将集成事件写入 Outbox 表（应与业务操作在同一数据库事务中调用）。
    /// </summary>
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = _idGen.Generator.NextId(),
            EventId = integrationEvent.EventId,
            EventType = integrationEvent.EventType,
            Payload = integrationEvent.ToPayload(),
            TenantId = integrationEvent.TenantId.Value,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxRetries = 5
        };

        await _outboxRepository.AddAsync(message, cancellationToken);
    }
}
