using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// Entry 节点：工作流入口，将初始输入写入 data bag。
/// </summary>
public sealed class EntryNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.Entry;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        // Entry 节点把所有 configs 中声明的输入字段从 data bag 透传
        foreach (var (field, _) in node.Configs)
        {
            var value = context.GetVariable($"entry.{field}") ?? context.GetVariable(field);
            context.SetOutput(node.Key, field, value);
        }

        return Task.FromResult(NodeExecutorResult.Default);
    }
}
