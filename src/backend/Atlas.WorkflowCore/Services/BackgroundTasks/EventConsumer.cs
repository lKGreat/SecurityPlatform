using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.BackgroundTasks;

/// <summary>
/// 事件消费者 - 处理事件并唤醒等待的工作流
/// </summary>
public class EventConsumer : QueueConsumer
{
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IDateTimeProvider _datetimeProvider;
    private readonly IGreyList _greylist;

    public EventConsumer(
        IQueueProvider queueProvider,
        IPersistenceProvider persistenceProvider,
        IDistributedLockProvider lockProvider,
        IDateTimeProvider datetimeProvider,
        IGreyList greylist,
        WorkflowOptions options,
        ILogger<EventConsumer> logger)
        : base(queueProvider, options, logger)
    {
        _persistenceProvider = persistenceProvider;
        _lockProvider = lockProvider;
        _datetimeProvider = datetimeProvider;
        _greylist = greylist;
    }

    protected override QueueType Queue => QueueType.Event;

    protected override async Task ProcessItem(string itemId, CancellationToken cancellationToken)
    {
        if (!await _lockProvider.AcquireLock($"evt:{itemId}", cancellationToken))
        {
            Logger.LogInformation($"Event locked {itemId}");
            return;
        }
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evt = await _persistenceProvider.GetEventAsync(itemId, cancellationToken);

            if (evt == null)
            {
                Logger.LogWarning("事件 {EventId} 不存在", itemId);
                return;
            }

            WorkflowActivityTracing.Enrich(evt);
            if (evt.IsProcessed)
            {
                _greylist.Add($"evt:{evt.Id}");
                return;
            }
            if (evt.EventTime <= _datetimeProvider.UtcNow)
            {
                IEnumerable<EventSubscription> subs;
                
                // 特殊处理ActivityResult
                if (evt.EventData is ActivityResult)
                {
                    var activity = await _persistenceProvider.GetEventSubscriptionAsync((evt.EventData as ActivityResult)!.SubscriptionId, cancellationToken);
                    if (activity == null)
                    {
                        Logger.LogWarning($"Activity already processed - {(evt.EventData as ActivityResult)!.SubscriptionId}");
                        await _persistenceProvider.MarkEventProcessedAsync(itemId, cancellationToken);
                        return;
                    }
                    subs = new List<EventSubscription> { activity };
                }
                else
                {
                    subs = await _persistenceProvider.GetEventSubscriptionsAsync(evt.EventName, evt.EventKey, evt.EventTime, cancellationToken);
                }

                var toQueue = new HashSet<string>();
                var complete = true;

                foreach (var sub in subs.ToList())
                    complete = complete && await SeedSubscription(evt, sub, toQueue, cancellationToken);

                if (complete)
                {
                    await _persistenceProvider.MarkEventProcessedAsync(itemId, cancellationToken);
                }
                else
                {
                    _greylist.Remove($"evt:{evt.Id}");
                }

                foreach (var eventId in toQueue)
                    await QueueProvider.QueueWork(eventId, QueueType.Event);
            }
        }
        finally
        {
            await _lockProvider.ReleaseLock($"evt:{itemId}");
        }
    }
    
    private async Task<bool> SeedSubscription(Event evt, EventSubscription sub, HashSet<string> toQueue, CancellationToken cancellationToken)
    {
        // 检查是否有早于当前事件的同类事件需要先处理
        var eventIds = await _persistenceProvider.GetEvents(sub.EventName, sub.EventKey, sub.SubscribeAsOf, cancellationToken);
        foreach (var eventId in eventIds)
        {
            if (eventId == evt.Id)
                continue;

            var siblingEvent = await _persistenceProvider.GetEventAsync(eventId, cancellationToken);
            if (siblingEvent == null)
                continue;
                
            if ((!siblingEvent.IsProcessed) && (siblingEvent.EventTime < evt.EventTime))
            {
                await QueueProvider.QueueWork(eventId, QueueType.Event);
                return false;
            }

            if (!siblingEvent.IsProcessed)
                toQueue.Add(siblingEvent.Id);
        }

        if (!await _lockProvider.AcquireLock(sub.WorkflowId, cancellationToken))
        {
            Logger.LogInformation("Workflow locked {WorkflowId}", sub.WorkflowId);
            return false;
        }
        
        try
        {
            var workflow = await _persistenceProvider.GetWorkflowAsync(sub.WorkflowId, cancellationToken);
            if (workflow == null)
            {
                Logger.LogWarning("Workflow {WorkflowId} not found", sub.WorkflowId);
                return false;
            }
            
            IEnumerable<ExecutionPointer> pointers;
            
            // 支持两种匹配方式：ExecutionPointerId 或 EventName+EventKey
            if (!string.IsNullOrEmpty(sub.ExecutionPointerId))
                pointers = workflow.ExecutionPointers.Where(p => p.Id == sub.ExecutionPointerId && !p.EventPublished && p.EndTime == null);
            else
                pointers = workflow.ExecutionPointers.Where(p => p.EventName == sub.EventName && p.EventKey == sub.EventKey && !p.EventPublished && p.EndTime == null);

            foreach (var p in pointers)
            {
                p.EventData = evt.EventData;
                p.EventPublished = true;
                p.Active = true;
            }
            workflow.NextExecution = 0;
            await _persistenceProvider.PersistWorkflowAsync(workflow, cancellationToken);
            await _persistenceProvider.TerminateEventSubscriptionAsync(sub.Id, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ex.Message);
            return false;
        }
        finally
        {
            await _lockProvider.ReleaseLock(sub.WorkflowId);
            await QueueProvider.QueueWork(sub.WorkflowId, QueueType.Workflow);
        }
    }
}
