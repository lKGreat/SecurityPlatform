using Atlas.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Events;

/// <summary>
/// 进程内事件总线。
/// 从 DI 容器解析所有已注册的 handler 并顺序执行。
/// 单个 handler 失败不会中断其他 handler 的执行；所有失败会在最终通过
/// <see cref="AggregateException"/> 聚合抛出，以保证调用方（如 Outbox 处理器）
/// 能感知到失败并触发重试/死信逻辑。
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(IServiceProvider serviceProvider, ILogger<InProcessEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        List<Exception>? failures = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Event handler {HandlerType} failed for event {EventType} (EventId={EventId})",
                    handler.GetType().Name,
                    typeof(TEvent).Name,
                    domainEvent.EventId);

                failures ??= [];
                failures.Add(ex);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more handlers failed for event {typeof(TEvent).Name} (EventId={domainEvent.EventId})",
                failures);
        }
    }
}
