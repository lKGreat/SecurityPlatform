using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.BackgroundTasks;

/// <summary>
/// 可运行实例轮询器 - 定时轮询可运行的工作流和事件
/// </summary>
public class RunnablePoller : IBackgroundTask
{
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly IQueueProvider _queueProvider;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGreyList _greyList;
    private readonly ILogger<RunnablePoller> _logger;
    private readonly TimeSpan _pollInterval;

    private Task? _runTask;
    private CancellationTokenSource? _cts;

    public RunnablePoller(
        IPersistenceProvider persistenceProvider,
        IQueueProvider queueProvider,
        IDistributedLockProvider lockProvider,
        IDateTimeProvider dateTimeProvider,
        IGreyList greyList,
        ILogger<RunnablePoller> logger)
    {
        _persistenceProvider = persistenceProvider;
        _queueProvider = queueProvider;
        _lockProvider = lockProvider;
        _dateTimeProvider = dateTimeProvider;
        _greyList = greyList;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(10); // 默认10秒轮询一次
    }

    public Task Start(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = Task.Run(() => Run(_cts.Token), _cts.Token);
        _logger.LogInformation("RunnablePoller 已启动");
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        if (_runTask != null)
        {
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException)
            {
                // 预期的取消异常
            }
        }

        _logger.LogInformation("RunnablePoller 已停止");
    }

    private async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollRunnableWorkflows(cancellationToken);
                await PollRunnableEvents(cancellationToken);
                await PollCommands(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunnablePoller 轮询时发生错误");
            }

            try
            {
                await Task.Delay(_pollInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollRunnableWorkflows(CancellationToken cancellationToken)
    {
        const string lockId = "poll-runnable-workflows";

        using var activity = WorkflowActivityTracing.StartPoll("workflows");

        var lockAcquired = await _lockProvider.AcquireLock(lockId, cancellationToken);

        if (!lockAcquired)
        {
            _logger.LogDebug("无法获取轮询工作流的锁，跳过此次轮询");
            return;
        }

        try
        {
            _logger.LogDebug("Polling for runnable workflows");

            var now = _dateTimeProvider.UtcNow;
            var runnableInstances = await _persistenceProvider.GetRunnableInstancesAsync(now, cancellationToken);

            var instances = runnableInstances.ToList();

            if (instances.Count > 0)
            {
                _logger.LogDebug("发现 {Count} 个可运行的工作流实例", instances.Count);

                foreach (var instance in instances)
                {
                    // 如果支持计划命令，优先使用ScheduledCommand
                    if (_persistenceProvider.SupportsScheduledCommands)
                    {
                        try
                        {
                            await _persistenceProvider.ScheduleCommandAsync(new ScheduledCommand
                            {
                                CommandName = ScheduledCommand.ProcessWorkflow,
                                Data = instance.Id,
                                ExecuteTime = _dateTimeProvider.UtcNow
                            }, cancellationToken);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
                        }
                    }

                    // 检查灰名单，避免重复入队
                    if (_greyList.Contains($"wf:{instance.Id}"))
                    {
                        _logger.LogDebug("工作流 {WorkflowId} 已在灰名单中，跳过", instance.Id);
                        continue;
                    }

                    _logger.LogDebug("工作流 {WorkflowId} 入队", instance.Id);
                    _greyList.Add($"wf:{instance.Id}");
                    await _queueProvider.QueueWork(instance.Id, QueueType.Workflow);
                }
            }
        }
        finally
        {
            await _lockProvider.ReleaseLock(lockId);
        }
    }

    private async Task PollRunnableEvents(CancellationToken cancellationToken)
    {
        const string lockId = "poll-runnable-events";

        using var activity = WorkflowActivityTracing.StartPoll("events");

        var lockAcquired = await _lockProvider.AcquireLock(lockId, cancellationToken);

        if (!lockAcquired)
        {
            _logger.LogDebug("无法获取轮询事件的锁，跳过此次轮询");
            return;
        }

        try
        {
            _logger.LogDebug("Polling for unprocessed events");

            var now = _dateTimeProvider.UtcNow;
            var runnableEvents = await _persistenceProvider.GetRunnableEventsAsync(now, cancellationToken);

            var events = runnableEvents.ToList();

            if (events.Count > 0)
            {
                _logger.LogDebug("发现 {Count} 个未处理的事件", events.Count);

                foreach (var eventId in events)
                {
                    // 如果支持计划命令，优先使用ScheduledCommand
                    if (_persistenceProvider.SupportsScheduledCommands)
                    {
                        try
                        {
                            await _persistenceProvider.ScheduleCommandAsync(new ScheduledCommand
                            {
                                CommandName = ScheduledCommand.ProcessEvent,
                                Data = eventId,
                                ExecuteTime = _dateTimeProvider.UtcNow
                            }, cancellationToken);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
                        }
                    }

                    // 检查灰名单，避免重复入队
                    if (_greyList.Contains($"evt:{eventId}"))
                    {
                        _logger.LogDebug("事件 {EventId} 已在灰名单中，跳过", eventId);
                        continue;
                    }

                    _logger.LogDebug("事件 {EventId} 入队", eventId);
                    _greyList.Add($"evt:{eventId}");
                    await _queueProvider.QueueWork(eventId, QueueType.Event);
                }
            }
        }
        finally
        {
            await _lockProvider.ReleaseLock(lockId);
        }
    }

    private async Task PollCommands(CancellationToken cancellationToken)
    {
        var activity = WorkflowActivityTracing.StartPoll("commands");
        try
        {
            if (!_persistenceProvider.SupportsScheduledCommands)
                return;

            if (await _lockProvider.AcquireLock("poll-commands", cancellationToken))
            {
                try
                {
                    _logger.LogDebug("Polling for scheduled commands");
                    await _persistenceProvider.ProcessCommands(new DateTimeOffset(_dateTimeProvider.UtcNow), async (command) =>
                    {
                        switch (command.CommandName)
                        {
                            case ScheduledCommand.ProcessWorkflow:
                                if (!string.IsNullOrEmpty(command.Data))
                                    await _queueProvider.QueueWork(command.Data, QueueType.Workflow);
                                break;
                            case ScheduledCommand.ProcessEvent:
                                if (!string.IsNullOrEmpty(command.Data))
                                    await _queueProvider.QueueWork(command.Data, QueueType.Event);
                                break;
                        }
                    }, cancellationToken);
                }
                finally
                {
                    await _lockProvider.ReleaseLock("poll-commands");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
        }
        finally
        {
            activity?.Dispose();
        }
    }
}
