namespace Atlas.Core.Events;

/// <summary>
/// 事件总线接口。
/// 进程内事件总线，将事件分发给所有已注册的 handler。
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布领域事件，异步通知所有已注册的 handler。
    /// 单个 handler 失败不影响其他 handler。
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
