using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// AssignVariable 节点：将表达式或引用的值赋给输出变量。
/// Config 结构：
/// {
///   "assignments": [
///     { "outputKey": "result", "valueRef": "llm_1.text" }
///   ]
/// }
/// </summary>
public sealed class AssignVariableNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.AssignVariable;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        if (!node.Configs.TryGetValue("assignments", out var assignObj) || assignObj is null)
            return Task.FromResult(NodeExecutorResult.Default);

        List<AssignmentItem>? assignments = null;
        if (assignObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            assignments = je.Deserialize<List<AssignmentItem>>();
        }

        if (assignments is null) return Task.FromResult(NodeExecutorResult.Default);

        foreach (var item in assignments)
        {
            var value = string.IsNullOrEmpty(item.ValueRef)
                ? item.LiteralValue
                : context.GetVariable(item.ValueRef);

            context.SetOutput(node.Key, item.OutputKey, value);
        }

        return Task.FromResult(NodeExecutorResult.Default);
    }

    private sealed class AssignmentItem
    {
        public string OutputKey { get; set; } = string.Empty;
        public string? ValueRef { get; set; }
        public string? LiteralValue { get; set; }
    }
}
