using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Services.DefaultProviders;

/// <summary>
/// 瞬态内存持久化提供者 - 包装单例内存提供者以支持瞬态注入
/// </summary>
public class TransientMemoryPersistenceProvider : IPersistenceProvider
{
    private readonly ISingletonMemoryProvider _innerService;

    public bool SupportsScheduledCommands => false;

    public TransientMemoryPersistenceProvider(ISingletonMemoryProvider innerService)
    {
        _innerService = innerService;
    }

    #region IWorkflowRepository

    public Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default) 
        => _innerService.CreateNewWorkflow(workflow, cancellationToken);

    public Task<string> CreateWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default) 
        => _innerService.CreateWorkflowAsync(workflow, cancellationToken);

    public Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default) 
        => _innerService.PersistWorkflow(workflow, cancellationToken);

    public Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default) 
        => _innerService.PersistWorkflow(workflow, subscriptions, cancellationToken);

    public Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default) 
        => _innerService.PersistWorkflowAsync(workflow, cancellationToken);

    public Task PersistWorkflowAsync(WorkflowInstance workflow, List<ExecutionPointer> pointers, CancellationToken cancellationToken = default) 
        => _innerService.PersistWorkflowAsync(workflow, pointers, cancellationToken);

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription>? subscriptions, CancellationToken cancellationToken = default)
    {
        await PersistWorkflow(workflow, cancellationToken);

        if (subscriptions != null)
        {
            foreach (var subscription in subscriptions)
            {
                await CreateEventSubscription(subscription, cancellationToken);
            }
        }
    }

    public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetRunnableInstances(asAt, cancellationToken);

    public Task<IEnumerable<WorkflowInstance>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetRunnableInstancesAsync(asAt, cancellationToken);

    public Task<WorkflowInstance> GetWorkflowInstance(string id, CancellationToken cancellationToken = default) 
        => _innerService.GetWorkflowInstance(id, cancellationToken);

    public Task<WorkflowInstance?> GetWorkflowAsync(string id, CancellationToken cancellationToken = default) 
        => _innerService.GetWorkflowAsync(id, cancellationToken);

    public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default) 
        => _innerService.GetWorkflowInstances(ids, cancellationToken);

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        return await GetWorkflowInstances(ids, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(WorkflowStatus? status, string? type, DateTime? createdFrom, DateTime? createdTo, int skip, int take, CancellationToken cancellationToken = default)
    {
        // 调用MemoryPersistenceProvider的方法
        if (_innerService is MemoryPersistenceProvider provider)
        {
            return await provider.GetWorkflowInstancesAsync(status, type, createdFrom, createdTo, skip, take, cancellationToken);
        }
        throw new NotImplementedException();
    }

    public Task TerminateWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
        => _innerService.TerminateWorkflowAsync(workflowId, cancellationToken);

    #endregion

    #region IEventRepository

    public Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken = default) 
        => _innerService.CreateEvent(newEvent, cancellationToken);

    public Task<string> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default) 
        => _innerService.CreateEventAsync(newEvent, cancellationToken);

    public Task<Event> GetEvent(string id, CancellationToken cancellationToken = default) 
        => _innerService.GetEvent(id, cancellationToken);

    public Task<Event?> GetEventAsync(string id, CancellationToken cancellationToken = default) 
        => _innerService.GetEventAsync(id, cancellationToken);

    public Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) 
        => _innerService.GetEvents(eventName, eventKey, asOf, cancellationToken);

    public Task<IEnumerable<Event>> GetEventsAsync(string eventName, string eventKey, DateTime? asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetEventsAsync(eventName, eventKey, asAt, cancellationToken);

    public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetRunnableEvents(asAt, cancellationToken);

    public Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetRunnableEventsAsync(asAt, cancellationToken);

    public Task MarkEventProcessed(string id, CancellationToken cancellationToken = default) 
        => _innerService.MarkEventProcessed(id, cancellationToken);

    public Task MarkEventProcessedAsync(string id, CancellationToken cancellationToken = default) 
        => _innerService.MarkEventProcessedAsync(id, cancellationToken);

    public Task MarkEventUnprocessed(string id, CancellationToken cancellationToken = default) 
        => _innerService.MarkEventUnprocessed(id, cancellationToken);

    public Task MarkEventUnprocessedAsync(string id, CancellationToken cancellationToken = default) 
        => _innerService.MarkEventUnprocessedAsync(id, cancellationToken);

    #endregion

    #region ISubscriptionRepository

    public Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken = default) 
        => _innerService.CreateEventSubscription(subscription, cancellationToken);

    public Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken cancellationToken = default) 
        => _innerService.CreateEventSubscriptionAsync(subscription, cancellationToken);

    public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) 
        => _innerService.GetSubscriptions(eventName, eventKey, asOf, cancellationToken);

    public Task<IEnumerable<EventSubscription>> GetEventSubscriptionsAsync(string eventName, string eventKey, DateTime? asAt, CancellationToken cancellationToken = default) 
        => _innerService.GetEventSubscriptionsAsync(eventName, eventKey, asAt, cancellationToken);

    public Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default) 
        => _innerService.TerminateSubscription(eventSubscriptionId, cancellationToken);

    public Task TerminateEventSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default) 
        => _innerService.TerminateEventSubscriptionAsync(eventSubscriptionId, cancellationToken);

    public Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default) 
        => _innerService.GetSubscription(eventSubscriptionId, cancellationToken);

    public Task<EventSubscription?> GetEventSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default) 
        => _innerService.GetEventSubscriptionAsync(eventSubscriptionId, cancellationToken);

    public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default) 
        => _innerService.GetFirstOpenSubscription(eventName, eventKey, asOf, cancellationToken);

    public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default) 
        => _innerService.SetSubscriptionToken(eventSubscriptionId, token, workerId, expiry, cancellationToken);

    public Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default) 
        => _innerService.ClearSubscriptionToken(eventSubscriptionId, token, cancellationToken);

    #endregion

    #region IScheduledCommandRepository

    public Task ScheduleCommand(ScheduledCommand command)
    {
        throw new NotImplementedException();
    }

    public Task ScheduleCommandAsync(ScheduledCommand command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Other

    public Task EnsureStoreExists(CancellationToken cancellationToken = default) 
        => _innerService.EnsureStoreExists(cancellationToken);

    public Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default) 
        => _innerService.PersistErrorsAsync(errors, cancellationToken);

    #endregion
}
