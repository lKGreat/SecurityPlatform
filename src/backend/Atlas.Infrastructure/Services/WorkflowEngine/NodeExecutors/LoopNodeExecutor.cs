using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 循环节点：支持计数循环。
/// Config 参数：maxIterations（默认 10）
/// 输出变量：loop_index、loop_completed
/// </summary>
public sealed class LoopNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Loop;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        int.TryParse(context.Node.Config.GetValueOrDefault("maxIterations"), out var maxIterations);
        if (maxIterations <= 0) maxIterations = 10;

        int.TryParse(context.Variables.GetValueOrDefault("loop_index"), out var currentIndex);

        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (currentIndex < maxIterations)
        {
            outputs["loop_index"] = (currentIndex + 1).ToString();
            outputs["loop_completed"] = "false";
        }
        else
        {
            outputs["loop_index"] = currentIndex.ToString();
            outputs["loop_completed"] = "true";
        }

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
