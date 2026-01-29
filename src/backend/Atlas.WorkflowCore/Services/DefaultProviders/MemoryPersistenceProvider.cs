using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Services.DefaultProviders;

public interface ISingletonMemoryProvider : IPersistenceProvider
{
}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

/// <summary>
/// 内存持久化提供者 - 用于演示和测试目的
/// </summary>
public class MemoryPersistenceProvider : ISingletonMemoryProvider
{
    private readonly List<WorkflowInstance> _instances = new List<WorkflowInstance>();
    private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
    private readonly List<Event> _events = new List<Event>();
    private readonly List<ExecutionError> _errors = new List<ExecutionError>();

    public bool SupportsScheduledCommands => false;

    public async Task EnsureStoreExists(CancellationToken cancellationToken = default)
    {
    }

    #region Workflow Operations

    public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            workflow.Id = Guid.NewGuid().ToString();
            _instances.Add(workflow);
            return workflow.Id;
        }
    }

    public async Task<string> CreateWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        return await CreateNewWorkflow(workflow, cancellationToken);
    }

    public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);
        }
    }

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, CancellationToken cancellationToken = default)
    {
        await PersistWorkflow(workflow, cancellationToken);
    }

    public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);

            lock (_subscriptions)
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Id = Guid.NewGuid().ToString();
                    _subscriptions.Add(subscription);
                }
            }
        }
    }

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, List<EventSubscription>? subscriptions, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var existing = _instances.First(x => x.Id == workflow.Id);
            _instances.Remove(existing);
            _instances.Add(workflow);

            if (subscriptions != null)
            {
                lock (_subscriptions)
                {
                    foreach (var subscription in subscriptions)
                    {
                        subscription.Id = Guid.NewGuid().ToString();
                        _subscriptions.Add(subscription);
                    }
                }
            }
        }
    }

    public async Task PersistWorkflowAsync(WorkflowInstance workflow, List<ExecutionPointer> pointers, CancellationToken cancellationToken = default)
    {
        await PersistWorkflowAsync(workflow, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var now = asAt.ToUniversalTime().Ticks;
            return _instances.Where(x => x.NextExecution.HasValue && x.NextExecution <= now).Select(x => x.Id).ToList();
        }
    }

    public async Task<IEnumerable<WorkflowInstance>> GetRunnableInstancesAsync(DateTime asAt, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var now = asAt.ToUniversalTime().Ticks;
            return _instances.Where(x => x.NextExecution.HasValue && x.NextExecution <= now).ToList();
        }
    }

    public async Task<WorkflowInstance> GetWorkflowInstance(string id, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            return _instances.First(x => x.Id == id);
        }
    }

    public async Task<WorkflowInstance?> GetWorkflowAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            return _instances.FirstOrDefault(x => x.Id == id);
        }
    }

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null)
        {
            return new List<WorkflowInstance>();
        }

        lock (_instances)
        {
            return _instances.Where(x => ids.Contains(x.Id)).ToList();
        }
    }

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null)
        {
            return new List<WorkflowInstance>();
        }

        lock (_instances)
        {
            return _instances.Where(x => ids.Contains(x.Id)).ToList();
        }
    }

    public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstancesAsync(WorkflowStatus? status, string? type, DateTime? createdFrom, DateTime? createdTo, int skip, int take, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var result = _instances.AsQueryable();

            if (status.HasValue)
            {
                result = result.Where(x => x.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                result = result.Where(x => x.WorkflowDefinitionId == type);
            }

            if (createdFrom.HasValue)
            {
                result = result.Where(x => x.CreateTime >= createdFrom.Value);
            }

            if (createdTo.HasValue)
            {
                result = result.Where(x => x.CreateTime <= createdTo.Value);
            }

            return result.Skip(skip).Take(take).ToList();
        }
    }

    public async Task TerminateWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        lock (_instances)
        {
            var workflow = _instances.FirstOrDefault(x => x.Id == workflowId);
            if (workflow != null)
            {
                workflow.Status = WorkflowStatus.Terminated;
                workflow.CompleteTime = DateTime.UtcNow;
            }
        }
    }

    #endregion

    #region Event Subscription Operations

    public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            subscription.Id = Guid.NewGuid().ToString();
            _subscriptions.Add(subscription);
            return subscription.Id;
        }
    }

    public async Task<string> CreateEventSubscriptionAsync(EventSubscription subscription, CancellationToken cancellationToken = default)
    {
        return await CreateEventSubscription(subscription, cancellationToken);
    }

    public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            return _subscriptions
                .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                .ToList();
        }
    }

    public async Task<IEnumerable<EventSubscription>> GetEventSubscriptionsAsync(string eventName, string eventKey, DateTime? asAt, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            return _subscriptions
                .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= (asAt ?? DateTime.UtcNow))
                .ToList();
        }
    }

    public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.SingleOrDefault(x => x.Id == eventSubscriptionId);
            if (sub != null)
                _subscriptions.Remove(sub);
        }
    }

    public async Task TerminateEventSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default)
    {
        await TerminateSubscription(eventSubscriptionId, cancellationToken);
    }

    public async Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.Single(x => x.Id == eventSubscriptionId);
            return sub;
        }
    }

    public async Task<EventSubscription?> GetEventSubscriptionAsync(string eventSubscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.SingleOrDefault(x => x.Id == eventSubscriptionId);
            return sub;
        }
    }

    public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var result = _subscriptions
                .First(x => x.ExternalToken == null && x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);
            return Task.FromResult(result);
        }
    }

    public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.SingleOrDefault(x => x.Id == eventSubscriptionId);
            if (sub != null)
            {
                sub.ExternalToken = token;
                sub.ExternalWorkerId = workerId;
                sub.ExternalTokenExpiry = expiry;
            }
            
            return Task.FromResult(true);
        }
    }

    public Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var sub = _subscriptions.SingleOrDefault(x => x.Id == eventSubscriptionId);
            if (sub != null)
            {
                if (sub.ExternalToken != token)
                    throw new InvalidOperationException();
                    
                sub.ExternalToken = null;
                sub.ExternalWorkerId = null;
                sub.ExternalTokenExpiry = null;
            }

            return Task.CompletedTask;
        }
    }

    #endregion

    #region Event Operations

    public async Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            _events.Add(newEvent);
            return newEvent.Id;
        }
    }

    public async Task<string> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default)
    {
        return await CreateEvent(newEvent, cancellationToken);
    }

    public async Task MarkEventProcessed(string id, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            var evt = _events.FirstOrDefault(x => x.Id == id);
            if (evt != null)
                evt.IsProcessed = true;
        }
    }

    public async Task MarkEventProcessedAsync(string id, CancellationToken cancellationToken = default)
    {
        await MarkEventProcessed(id, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            return _events
                .Where(x => !x.IsProcessed)
                .Where(x => x.EventTime <= asAt.ToUniversalTime())
                .Select(x => x.Id)
                .ToList();
        }
    }

    public async Task<IEnumerable<string>> GetRunnableEventsAsync(DateTime asAt, CancellationToken cancellationToken = default)
    {
        return await GetRunnableEvents(asAt, cancellationToken);
    }

    public async Task<Event> GetEvent(string id, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            return _events.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException($"Event {id} not found");
        }
    }

    public async Task<Event?> GetEventAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            return _events.FirstOrDefault(x => x.Id == id);
        }
    }

    public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            return _events
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= asOf)
                .Select(x => x.Id)
                .ToList();
        }
    }

    public async Task<IEnumerable<Event>> GetEventsAsync(string eventName, string eventKey, DateTime? asAt, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            return _events
                .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                .Where(x => x.EventTime >= (asAt ?? DateTime.MinValue))
                .ToList();
        }
    }

    public Task MarkEventUnprocessed(string id, CancellationToken cancellationToken = default)
    {
        lock (_events)
        {
            var evt = _events.FirstOrDefault(x => x.Id == id);
            if (evt != null)
            {
                evt.IsProcessed = false;
            }
        }
        return Task.CompletedTask;
    }

    public async Task MarkEventUnprocessedAsync(string id, CancellationToken cancellationToken = default)
    {
        await MarkEventUnprocessed(id, cancellationToken);
    }

    #endregion

    #region Error Operations

    public async Task PersistErrorsAsync(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default)
    {
        lock (_errors)
        {
            _errors.AddRange(errors);
        }
    }

    #endregion

    #region Scheduled Command Operations

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
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
