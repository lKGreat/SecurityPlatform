using Atlas.Core.Abstractions;
using Atlas.Core.Saga;
using Atlas.Domain.Saga;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Atlas.Infrastructure.Saga;

/// <summary>
/// Saga 编排器：顺序执行步骤，失败时反向补偿，每步状态持久化
/// </summary>
public sealed class SagaOrchestrator : ISagaOrchestrator
{
    private readonly ISqlSugarClient _db;
    private readonly IIdGeneratorAccessor _idGen;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(ISqlSugarClient db, IIdGeneratorAccessor idGen, ILogger<SagaOrchestrator> logger)
    {
        _db = db;
        _idGen = idGen;
        _logger = logger;
    }

    public async Task RunAsync<TContext>(
        IReadOnlyList<ISagaStep<TContext>> steps,
        TContext context,
        CancellationToken cancellationToken)
    {
        var sagaId = _idGen.Generator.NextId();
        var sagaType = typeof(TContext).Name;
        var now = DateTimeOffset.UtcNow;

        var instance = new SagaInstance
        {
            Id = sagaId,
            SagaType = sagaType,
            Status = SagaStatus.Running,
            CurrentStep = 0,
            ContextJson = System.Text.Json.JsonSerializer.Serialize(context),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _db.Insertable(instance).ExecuteCommandAsync(cancellationToken);

        var executedSteps = new List<ISagaStep<TContext>>();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            _logger.LogInformation("Saga {SagaId} executing step {Index}: {Step}", sagaId, i, step.StepName);

            try
            {
                await step.ExecuteAsync(context, cancellationToken);
                executedSteps.Add(step);

                await _db.Updateable<SagaInstance>()
                    .SetColumns(s => new SagaInstance { CurrentStep = i + 1, UpdatedAt = DateTimeOffset.UtcNow })
                    .Where(s => s.Id == sagaId)
                    .ExecuteCommandAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga {SagaId} step {Step} failed, starting compensation", sagaId, step.StepName);

                await _db.Updateable<SagaInstance>()
                    .SetColumns(s => new SagaInstance
                    {
                        Status = SagaStatus.Compensating,
                        ErrorMessage = ex.Message,
                        UpdatedAt = DateTimeOffset.UtcNow
                    })
                    .Where(s => s.Id == sagaId)
                    .ExecuteCommandAsync(cancellationToken);

                await CompensateAsync(sagaId, executedSteps, context, cancellationToken);
                throw;
            }
        }

        await _db.Updateable<SagaInstance>()
            .SetColumns(s => new SagaInstance { Status = SagaStatus.Completed, UpdatedAt = DateTimeOffset.UtcNow })
            .Where(s => s.Id == sagaId)
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
    }

    private async Task CompensateAsync<TContext>(
        long sagaId,
        List<ISagaStep<TContext>> executedSteps,
        TContext context,
        CancellationToken cancellationToken)
    {
        for (var i = executedSteps.Count - 1; i >= 0; i--)
        {
            var step = executedSteps[i];
            try
            {
                _logger.LogInformation("Saga {SagaId} compensating step {Step}", sagaId, step.StepName);
                await step.CompensateAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga {SagaId} compensation of step {Step} failed", sagaId, step.StepName);
            }
        }

        await _db.Updateable<SagaInstance>()
            .SetColumns(s => new SagaInstance { Status = SagaStatus.Failed, UpdatedAt = DateTimeOffset.UtcNow })
            .Where(s => s.Id == sagaId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
