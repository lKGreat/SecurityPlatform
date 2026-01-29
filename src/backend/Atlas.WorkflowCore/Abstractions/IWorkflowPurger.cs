using System;
using System.Threading;
using System.Threading.Tasks;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流清理器接口 - 用于清理旧的工作流实例
/// </summary>
public interface IWorkflowPurger
{
    /// <summary>
    /// 清理指定状态的旧工作流实例
    /// </summary>
    /// <param name="status">工作流状态</param>
    /// <param name="olderThan">早于此时间的工作流将被清理</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default);
}
