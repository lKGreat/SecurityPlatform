using System.Collections.Concurrent;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// 管理 Workflow V2 运行实例的取消令牌，用于跨请求取消后台执行任务。
/// </summary>
public sealed class WorkflowExecutionCancellationRegistry
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _runningExecutions = new();

    public bool Register(long executionId, CancellationTokenSource cancellationTokenSource)
    {
        return _runningExecutions.TryAdd(executionId, cancellationTokenSource);
    }

    public bool TryCancel(long executionId)
    {
        if (_runningExecutions.TryGetValue(executionId, out var cts))
        {
            cts.Cancel();
            return true;
        }

        return false;
    }

    public void Unregister(long executionId)
    {
        if (_runningExecutions.TryRemove(executionId, out var cts))
        {
            cts.Dispose();
        }
    }
}
