using Atlas.Domain.Events;

namespace Atlas.Application.Events;

public interface IEventSubscriptionService
{
    Task<IReadOnlyList<EventSubscription>> GetAllAsync(CancellationToken cancellationToken);
    Task<EventSubscription?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(CreateEventSubscriptionRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(long id, UpdateEventSubscriptionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);

    /// <summary>获取匹配指定事件类型的所有活跃订阅</summary>
    Task<IReadOnlyList<EventSubscription>> GetMatchingAsync(string eventType, CancellationToken cancellationToken);
}

public sealed record CreateEventSubscriptionRequest(
    string Name,
    string EventTypePattern,
    EventSubscriptionTargetType TargetType,
    string TargetConfig,
    string? FilterExpression);

public sealed record UpdateEventSubscriptionRequest(
    string Name,
    string EventTypePattern,
    EventSubscriptionTargetType TargetType,
    string TargetConfig,
    string? FilterExpression,
    bool IsActive);
