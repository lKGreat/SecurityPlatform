using Atlas.Application.Workflow.Models.V2;

namespace Atlas.Application.Workflow.Abstractions.V2;

/// <summary>
/// Coze 风格工作流执行服务（v2）：同步/异步/流式执行，中断恢复，单节点调试
/// </summary>
public interface IWorkflowV2ExecutionService
{
    /// <summary>同步执行，等待工作流完成后返回结果</summary>
    Task<WorkflowRunResponse> SyncRunAsync(long workflowId, WorkflowRunRequest request, CancellationToken cancellationToken);

    /// <summary>异步执行，立即返回 ExecutionId，通过 GetExecutionProcess 轮询</summary>
    Task<long> AsyncRunAsync(long workflowId, WorkflowRunRequest request, CancellationToken cancellationToken);

    /// <summary>取消执行</summary>
    Task CancelAsync(long executionId, CancellationToken cancellationToken);

    /// <summary>中断恢复（用于 QuestionAnswer 等待用户输入场景）</summary>
    Task ResumeAsync(long executionId, WorkflowResumeRequest request, CancellationToken cancellationToken);

    /// <summary>单节点调试，在工作流画布中孤立执行单个节点</summary>
    Task<NodeDebugResponse> DebugNodeAsync(long workflowId, NodeDebugRequest request, CancellationToken cancellationToken);

    /// <summary>流式执行，返回 SSE 事件流（IAsyncEnumerable）</summary>
    IAsyncEnumerable<SseEvent> StreamRunAsync(long workflowId, WorkflowRunRequest request, CancellationToken cancellationToken);
}
