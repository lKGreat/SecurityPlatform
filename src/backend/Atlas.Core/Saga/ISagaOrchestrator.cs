namespace Atlas.Core.Saga;

public interface ISagaOrchestrator
{
    /// <summary>顺序执行所有步骤，任意步骤失败时反向补偿</summary>
    Task RunAsync<TContext>(
        IReadOnlyList<ISagaStep<TContext>> steps,
        TContext context,
        CancellationToken cancellationToken);
}
