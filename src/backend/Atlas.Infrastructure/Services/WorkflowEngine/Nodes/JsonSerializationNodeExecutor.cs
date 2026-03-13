using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// JsonSerialization 节点：将对象序列化为 JSON 字符串。
/// Config 结构：{ "valueRef": "llm_1.data", "pretty": false }
/// </summary>
public sealed class JsonSerializationNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.JsonSerialization;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var valueRef = node.Configs.TryGetValue("valueRef", out var vr) ? vr?.ToString() : null;
        var pretty = node.Configs.TryGetValue("pretty", out var p) && p?.ToString() == "true";

        var value = string.IsNullOrEmpty(valueRef)
            ? null
            : context.GetVariable(valueRef);

        var options = pretty
            ? new JsonSerializerOptions { WriteIndented = true }
            : JsonSerializerOptions.Default;

        var json = JsonSerializer.Serialize(value, options);
        context.SetOutput(node.Key, "json", json);

        return Task.FromResult(NodeExecutorResult.Default);
    }
}
