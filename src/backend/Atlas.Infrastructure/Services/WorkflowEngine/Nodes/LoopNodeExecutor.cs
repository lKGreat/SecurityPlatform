using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// Loop 节点：对数组中每个元素迭代执行 body 分支。
/// Config 结构：{ "arrayRef": "entry_1.items" }
/// 通过特殊端口 "body" 触发循环体，"done" 表示循环结束。
/// </summary>
public sealed class LoopNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.Loop;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var arrayRef = node.Configs.TryGetValue("arrayRef", out var ar) ? ar?.ToString() : null;
        if (string.IsNullOrEmpty(arrayRef))
        {
            context.SetOutput(node.Key, "items", Array.Empty<object?>());
            context.SetOutput(node.Key, "currentIndex", 0);
            return Task.FromResult(NodeExecutorResult.Port("done"));
        }

        var raw = context.GetVariable(arrayRef);
        List<object?> items;

        if (raw is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            items = je.EnumerateArray().Select(e => (object?)e).ToList();
        }
        else if (raw is IEnumerable<object?> enumerable)
        {
            items = enumerable.ToList();
        }
        else
        {
            items = new List<object?> { raw };
        }

        context.SetOutput(node.Key, "items", items);
        context.SetOutput(node.Key, "currentIndex", 0);
        context.SetOutput(node.Key, "currentItem", items.FirstOrDefault());
        context.SetOutput(node.Key, "count", items.Count);

        return Task.FromResult(items.Count > 0
            ? NodeExecutorResult.Port("body")
            : NodeExecutorResult.Port("done"));
    }
}
