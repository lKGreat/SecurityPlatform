using System.Text.Json;
using Atlas.Application.Approval.Abstractions;
using Atlas.Core.Messaging;
using Atlas.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 审批事件异步消费者：从消息队列消费审批事件，分发给 IApprovalEventHandler
/// 通过 Messaging:ApprovalEvents:Enabled 配置控制是否启用异步模式
/// </summary>
public sealed class ApprovalEventConsumer : IQueueMessageHandler
{
    public static readonly string QueueName = "approval-events";
    string IQueueMessageHandler.QueueName => QueueName;
    string? IQueueMessageHandler.MessageType => null;

    private readonly IEnumerable<IApprovalEventHandler> _handlers;
    private readonly ILogger<ApprovalEventConsumer> _logger;

    public ApprovalEventConsumer(
        IEnumerable<IApprovalEventHandler> handlers,
        ILogger<ApprovalEventConsumer> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task HandleAsync(QueueMessageItem message, CancellationToken cancellationToken)
    {
        ApprovalEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ApprovalEventEnvelope>(message.Payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize approval event payload for message {Id}", message.Id);
            return;
        }

        if (envelope is null) return;

        var e = new ApprovalInstanceEvent(
            TenantId: new Atlas.Core.Tenancy.TenantId(envelope.TenantId),
            InstanceId: envelope.InstanceId,
            DefinitionId: envelope.DefinitionId,
            BusinessKey: envelope.BusinessKey,
            DataJson: envelope.DataJson,
            ActorUserId: envelope.ActorUserId);

        foreach (var handler in _handlers)
        {
            try
            {
                switch (envelope.EventType)
                {
                    case "InstanceStarted":
                        await handler.OnInstanceStartedAsync(e, cancellationToken);
                        break;
                    case "InstanceCompleted":
                        await handler.OnInstanceCompletedAsync(e, cancellationToken);
                        break;
                    case "InstanceRejected":
                        await handler.OnInstanceRejectedAsync(e, cancellationToken);
                        break;
                    case "InstanceCanceled":
                        await handler.OnInstanceCanceledAsync(e, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApprovalEventConsumer handler failed for event {EventType}", envelope.EventType);
                throw;
            }
        }
    }
}

public sealed record ApprovalEventEnvelope(
    Guid TenantId,
    long InstanceId,
    long DefinitionId,
    string BusinessKey,
    string? DataJson,
    long ActorUserId,
    string EventType);
