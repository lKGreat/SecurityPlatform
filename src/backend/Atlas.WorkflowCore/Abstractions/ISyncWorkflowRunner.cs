using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 同步工作流运行器接口 - 同步等待工作流完成
/// </summary>
public interface ISyncWorkflowRunner
{
    /// <summary>
    /// 同步运行工作流
    /// </summary>
    /// <typeparam name="TData">工作流数据类型</typeparam>
    /// <param name="workflowId">工作流ID</param>
    /// <param name="version">版本</param>
    /// <param name="data">工作流数据</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工作流实例</returns>
    Task<WorkflowInstance> RunWorkflow<TData>(
        string workflowId,
        int? version,
        TData data,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TData : class, new();
}
