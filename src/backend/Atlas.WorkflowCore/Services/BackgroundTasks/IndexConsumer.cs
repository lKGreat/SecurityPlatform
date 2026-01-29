using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Atlas.WorkflowCore.Services.BackgroundTasks;

/// <summary>
/// 索引消费者 - 更新工作流搜索索引
/// </summary>
public class IndexConsumer : QueueConsumer
{
    private readonly ISearchIndex _searchIndex;
    private readonly ObjectPool<IPersistenceProvider> _persistenceStorePool;
    private readonly Dictionary<string, int> _errorCounts = new Dictionary<string, int>();

    protected override bool EnableSecondPasses => true;

    public IndexConsumer(
        IQueueProvider queueProvider,
        IPooledObjectPolicy<IPersistenceProvider> persistencePoolPolicy,
        ISearchIndex searchIndex,
        WorkflowOptions options,
        ILogger<IndexConsumer> logger)
        : base(queueProvider, options, logger)
    {
        _persistenceStorePool = new DefaultObjectPool<IPersistenceProvider>(persistencePoolPolicy);
        _searchIndex = searchIndex;
    }

    protected override QueueType Queue => QueueType.Index;

    protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await FetchWorkflow(itemId, cancellationToken);
            
            WorkflowActivityTracing.Enrich(workflow, "index");
            await _searchIndex.IndexWorkflow(workflow);
            lock (_errorCounts)
            {
                _errorCounts.Remove(itemId);
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(default(EventId), $"Error indexing workflow - {itemId} - {e.Message}");
            var errCount = 0;
            lock (_errorCounts)
            {
                if (!_errorCounts.ContainsKey(itemId))
                    _errorCounts.Add(itemId, 0);

                _errorCounts[itemId]++;
                errCount = _errorCounts[itemId];
            }
            
            if (errCount < 5)
            {
                await QueueProvider.QueueWork(itemId, Queue);
                return;
            }
            if (errCount < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await QueueProvider.QueueWork(itemId, Queue);
                return;
            }

            lock (_errorCounts)
            {
                _errorCounts.Remove(itemId);
            }

            Logger.LogError(default(EventId), e, $"Unable to index workflow - {itemId} - {e.Message}");
        }
    }

    private async Task<WorkflowInstance> FetchWorkflow(string id, CancellationToken cancellationToken)
    {
        var store = _persistenceStorePool.Get();
        try
        {
            var workflow = await store.GetWorkflowAsync(id, cancellationToken);
            return workflow ?? throw new InvalidOperationException($"Workflow {id} not found");
        }
        finally
        {
            _persistenceStorePool.Return(store);
        }
    }
}
