namespace Atlas.Core.Saga;

/// <summary>
/// Saga 步骤接口：每个步骤可执行正向操作和补偿操作
/// </summary>
public interface ISagaStep<TContext>
{
    string StepName { get; }

    /// <summary>正向执行</summary>
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken);

    /// <summary>补偿（回滚）</summary>
    Task CompensateAsync(TContext context, CancellationToken cancellationToken);
}
