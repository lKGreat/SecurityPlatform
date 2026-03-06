namespace Atlas.Core.Events;

/// <summary>
/// 领域事件处理器接口。
/// 每个有界上下文通过实现此接口来响应特定领域事件。
/// </summary>
/// <typeparam name="TEvent">事件类型，必须实现 <see cref="IDomainEvent"/></typeparam>
public interface IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// 处理领域事件。
    /// 实现应当幂等：同一事件被重复投递时不产生副作用。
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
