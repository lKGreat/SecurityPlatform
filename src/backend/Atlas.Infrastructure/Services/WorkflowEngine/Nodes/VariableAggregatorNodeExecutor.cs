using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// VariableAggregator 节点：将多个上游变量合并成一个对象或数组。
/// Config 结构：
/// {
///   "mode": "object",  // "object" | "array"
///   "fields": [
///     { "key": "name", "ref": "llm_1.text" }
///   ]
/// }
/// </summary>
public sealed class VariableAggregatorNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.VariableAggregator;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var mode = node.Configs.TryGetValue("mode", out var m) ? m?.ToString() ?? "object" : "object";

        List<FieldMapping>? fields = null;
        if (node.Configs.TryGetValue("fields", out var fieldsObj) &&
            fieldsObj is System.Text.Json.JsonElement je &&
            je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            fields = je.Deserialize<List<FieldMapping>>();
        }

        if (fields is null || fields.Count == 0)
        {
            context.SetOutput(node.Key, "result", null);
            return Task.FromResult(NodeExecutorResult.Default);
        }

        if (mode == "array")
        {
            var arr = fields.Select(f => context.GetVariable(f.Ref)).ToList();
            context.SetOutput(node.Key, "result", arr);
        }
        else
        {
            var obj = fields.ToDictionary(f => f.Key, f => context.GetVariable(f.Ref));
            context.SetOutput(node.Key, "result", obj);
        }

        return Task.FromResult(NodeExecutorResult.Default);
    }

    private sealed class FieldMapping
    {
        public string Key { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
    }
}
