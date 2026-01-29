using System.Collections.Concurrent;
using System.Diagnostics;
using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.BackgroundTasks;

/// <summary>
/// 队列消费者抽象基类
/// </summary>
public abstract class QueueConsumer : IBackgroundTask
{
    protected abstract QueueType Queue { get; }
    protected virtual int MaxConcurrentItems => Math.Max(Environment.ProcessorCount, 2);
    protected virtual bool EnableSecondPasses => false;

    protected readonly IQueueProvider QueueProvider;
    protected readonly ILogger Logger;
    protected readonly WorkflowOptions Options;
    protected Task? DispatchTask;        
    private CancellationTokenSource? _cancellationTokenSource;
    private Dictionary<string, EventWaitHandle> _activeTasks;
    private List<Task> _runningTasks;
    private readonly object _runningTasksLock = new object();
    private ConcurrentDictionary<string, bool> _secondPasses;

    protected QueueConsumer(IQueueProvider queueProvider, WorkflowOptions options, ILogger logger)
    {
        QueueProvider = queueProvider;
        Options = options;
        Logger = logger;

        _activeTasks = new Dictionary<string, EventWaitHandle>();
        _runningTasks = new List<Task>();
        _secondPasses = new ConcurrentDictionary<string, bool>();
    }

    protected abstract Task ProcessItem(string itemId, CancellationToken cancellationToken);

    public virtual Task Start(CancellationToken cancellationToken = default)
    {
        if (DispatchTask != null)
        {
            throw new InvalidOperationException();
        }

        _cancellationTokenSource = new CancellationTokenSource();

        DispatchTask = Task.Factory.StartNew(Execute, TaskCreationOptions.LongRunning);
        
        return Task.CompletedTask;
    }

    public virtual async Task Stop()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
        
        if (DispatchTask != null)
        {
            await DispatchTask;
            DispatchTask = null;
        }
    }

    private async Task Execute()
    {
        var cancelToken = _cancellationTokenSource!.Token;            

        while (!cancelToken.IsCancellationRequested)
        {
            Activity? activity = default;
            try
            {
                var activeCount = 0;
                lock (_activeTasks)
                {
                    activeCount = _activeTasks.Count;
                }
                if (activeCount >= MaxConcurrentItems)
                {
                    await Task.Delay(Options.IdleTime);
                    continue;
                }

                activity = WorkflowActivityTracing.StartConsume(Queue);
                var item = await QueueProvider.DequeueWork(Queue, cancelToken);

                if (item == null)
                {
                    activity?.Dispose();
                    if (!QueueProvider.IsDequeueBlocking)
                        await Task.Delay(Options.IdleTime, cancelToken);
                    continue;
                }

                activity?.EnrichWithDequeuedItem(item);

                var hasTask = false;
                lock (_activeTasks)
                {
                    hasTask = _activeTasks.ContainsKey(item);
                }
                if (hasTask)
                {
                    _secondPasses.TryAdd(item, true);
                    if (!EnableSecondPasses)
                        await QueueProvider.QueueWork(item, Queue);
                    activity?.Dispose();
                    continue;
                }

                _secondPasses.TryRemove(item, out _);

                var waitHandle = new ManualResetEvent(false);
                lock (_activeTasks)
                {
                    _activeTasks.Add(item, waitHandle);
                }
                var task = ExecuteItem(item, waitHandle, activity);
                lock (_runningTasksLock)
                {
                    _runningTasks.Add(task);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                activity?.SetStatus(ActivityStatusCode.Error);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        List<EventWaitHandle> toComplete;
        lock (_activeTasks)
        {
            toComplete = _activeTasks.Values.ToList();
        }

        foreach (var handle in toComplete)
            handle.WaitOne();

        // Also await all running tasks to ensure proper async completion
        Task[] tasksToAwait;
        lock (_runningTasksLock)
        {
            tasksToAwait = _runningTasks.ToArray();
        }

        if (tasksToAwait.Length > 0)
        {
            try
            {
                await Task.WhenAll(tasksToAwait);
            }
            catch
            {
                // Individual task exceptions are already logged in ExecuteItem
            }
        }
    }

    private async Task ExecuteItem(string itemId, EventWaitHandle waitHandle, Activity? activity)
    {
        try
        {
            await ProcessItem(itemId, _cancellationTokenSource!.Token);
            while (EnableSecondPasses && _secondPasses.ContainsKey(itemId))
            {
                _secondPasses.TryRemove(itemId, out _);
                await ProcessItem(itemId, _cancellationTokenSource!.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation($"Operation cancelled while processing {itemId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error executing item {itemId} - {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error);
        }
        finally
        {
            waitHandle.Set();
            lock (_activeTasks)
            {
                _activeTasks.Remove(itemId);
            }
        }
    }
}
