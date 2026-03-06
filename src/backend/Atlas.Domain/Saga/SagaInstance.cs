namespace Atlas.Domain.Saga;

/// <summary>
/// Saga 实例持久化记录
/// </summary>
public sealed class SagaInstance
{
    public long Id { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public SagaStatus Status { get; set; } = SagaStatus.Running;
    public string ContextJson { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum SagaStatus
{
    Running = 0,
    Completed = 1,
    Compensating = 2,
    Failed = 3
}
