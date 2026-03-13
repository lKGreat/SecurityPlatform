using System.Text.Json;
using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 变量聚合节点：将多个变量合并为一个 JSON 对象。
/// Config 参数：variableKeys（逗号分隔的变量名列表）、outputKey（默认 "aggregated"）
/// </summary>
public sealed class VariableAggregatorNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.VariableAggregator;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var keys = (context.Node.Config.GetValueOrDefault("variableKeys") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var outputKey = context.Node.Config.GetValueOrDefault("outputKey") ?? "aggregated";

        var aggregated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (context.Variables.TryGetValue(key, out var value))
            {
                aggregated[key] = value;
            }
        }

        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [outputKey] = JsonSerializer.Serialize(aggregated)
        };

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
