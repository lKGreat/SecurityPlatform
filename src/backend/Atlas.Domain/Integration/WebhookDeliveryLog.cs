namespace Atlas.Domain.Integration;

/// <summary>
/// Webhook 投递日志
/// </summary>
public sealed class WebhookDeliveryLog
{
    public long Id { get; set; }
    public long SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
