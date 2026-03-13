using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// Batch 节点：对数组元素并行执行（与 Loop 不同，Batch 是并行的）。
/// Config 结构：{ "arrayRef": "entry_1.items", "concurrency": 5 }
/// 将 items 和相关信息写入输出，实际并行化由 DagExecutor 在 body 分支处理。
/// </summary>
public sealed class BatchNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.Batch;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var arrayRef = node.Configs.TryGetValue("arrayRef", out var ar) ? ar?.ToString() : null;

        if (string.IsNullOrEmpty(arrayRef))
        {
            context.SetOutput(node.Key, "results", Array.Empty<object?>());
            context.SetOutput(node.Key, "count", 0);
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
        context.SetOutput(node.Key, "count", items.Count);

        return Task.FromResult(NodeExecutorResult.Port("body"));
    }
}
