using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 子工作流节点：当前版本为占位实现，后续需要递归调用 DagExecutor。
/// Config 参数：workflowId
/// </summary>
public sealed class SubWorkflowNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.SubWorkflow;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var workflowId = context.Node.Config.GetValueOrDefault("workflowId");
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["subworkflow_id"] = workflowId ?? string.Empty,
            ["subworkflow_status"] = "not_implemented"
        };

        // 占位：TODO[coze-v2-subworkflow] 递归调用 DagExecutor 执行子工作流
        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
