using Atlas.Domain.Integration;

namespace Atlas.Application.Integration;

public interface IWebhookService
{
    Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken);
    Task<WebhookSubscription?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(CreateWebhookRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(long id, UpdateWebhookRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WebhookDeliveryLog>> GetDeliveriesAsync(long subscriptionId, int pageSize, CancellationToken cancellationToken);

    /// <summary>投递事件到所有匹配的订阅</summary>
    Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken);

    /// <summary>测试投递（立即发送一次 ping payload）</summary>
    Task TestDeliveryAsync(long subscriptionId, CancellationToken cancellationToken);
}

public sealed record CreateWebhookRequest(
    string Name,
    List<string> EventTypes,
    string TargetUrl,
    string Secret,
    Dictionary<string, string>? Headers);

public sealed record UpdateWebhookRequest(
    string Name,
    List<string> EventTypes,
    string TargetUrl,
    bool IsActive,
    Dictionary<string, string>? Headers);
