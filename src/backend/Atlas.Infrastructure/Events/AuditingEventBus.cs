using Atlas.Application.Audit.Abstractions;
using Atlas.Core.Events;
using Atlas.Core.Tenancy;
using Atlas.Domain.Audit.Entities;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Events;

/// <summary>
/// 集成事件审计装饰器：在每次事件发布前后写入审计日志
/// </summary>
public sealed class AuditingEventBus : IEventBus
{
    private readonly IEventBus _inner;
    private readonly IAuditWriter _auditWriter;
    private readonly ILogger<AuditingEventBus> _logger;

    public AuditingEventBus(IEventBus inner, IAuditWriter auditWriter, ILogger<AuditingEventBus> logger)
    {
        _inner = inner;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent).Name;
        var tenantId = domainEvent.TenantId;

        try
        {
            await _inner.PublishAsync(domainEvent, cancellationToken);

            var record = new AuditRecord(
                tenantId,
                actor: "System",
                action: $"Integration.{eventType}.Published",
                result: "Success",
                target: domainEvent.EventId.ToString(),
                ipAddress: null,
                userAgent: null);

            await _auditWriter.WriteAsync(record, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event {EventType} publication failed", eventType);

            var record = new AuditRecord(
                tenantId,
                actor: "System",
                action: $"Integration.{eventType}.PublishFailed",
                result: "Failed",
                target: domainEvent.EventId.ToString(),
                ipAddress: null,
                userAgent: null);

            await _auditWriter.WriteAsync(record, cancellationToken);
            throw;
        }
    }
}
