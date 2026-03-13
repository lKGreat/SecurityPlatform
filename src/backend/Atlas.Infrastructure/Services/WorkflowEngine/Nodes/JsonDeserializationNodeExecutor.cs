using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// JsonDeserialization 节点：将 JSON 字符串反序列化为对象。
/// Config 结构：{ "jsonRef": "http_1.body" }
/// </summary>
public sealed class JsonDeserializationNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.JsonDeserialization;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var jsonRef = node.Configs.TryGetValue("jsonRef", out var jr) ? jr?.ToString() : null;
        var jsonStr = string.IsNullOrEmpty(jsonRef)
            ? null
            : context.GetVariable(jsonRef)?.ToString();

        if (string.IsNullOrWhiteSpace(jsonStr))
        {
            context.SetOutput(node.Key, "data", null);
            context.SetOutput(node.Key, "success", false);
            return Task.FromResult(NodeExecutorResult.Default);
        }

        try
        {
            var obj = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            context.SetOutput(node.Key, "data", obj);
            context.SetOutput(node.Key, "success", true);
        }
        catch (JsonException)
        {
            context.SetOutput(node.Key, "data", null);
            context.SetOutput(node.Key, "success", false);
            context.SetOutput(node.Key, "error", "JSON 解析失败");
        }

        return Task.FromResult(NodeExecutorResult.Default);
    }
}
