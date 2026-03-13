using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// Exit 节点：工作流出口，收集最终输出并终止执行。
/// </summary>
public sealed class ExitNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.Exit;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        // 将 InputMappings 中引用的上游变量写入本节点输出
        foreach (var (field, reference) in node.InputMappings)
        {
            var value = context.GetVariable(reference);
            context.SetOutput(node.Key, field, value);
        }

        return Task.FromResult(NodeExecutorResult.Done);
    }
}
