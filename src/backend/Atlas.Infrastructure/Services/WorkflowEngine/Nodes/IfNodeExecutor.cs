using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// If 节点：条件分支，支持多条件组合（AND/OR），返回 "true" 或 "false" 端口。
/// Config 结构：
/// {
///   "conditions": [ { "left": "entry_1.score", "op": "gt", "right": "60" } ],
///   "logic": "and"   // "and" | "or"
/// }
/// </summary>
public sealed class IfNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.If;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var conditions = GetConditions(node);
        var logic = node.Configs.TryGetValue("logic", out var logicObj)
            ? logicObj?.ToString() ?? "and"
            : "and";

        bool result;
        if (logic.Equals("or", StringComparison.OrdinalIgnoreCase))
        {
            result = conditions.Any(c => EvaluateCondition(c, context));
        }
        else
        {
            result = conditions.All(c => EvaluateCondition(c, context));
        }

        context.SetOutput(node.Key, "result", result);
        return Task.FromResult(NodeExecutorResult.Port(result ? "true" : "false"));
    }

    private static List<ConditionItem> GetConditions(NodeSchema node)
    {
        if (!node.Configs.TryGetValue("conditions", out var condObj) || condObj is null)
            return new List<ConditionItem>();

        if (condObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return je.Deserialize<List<ConditionItem>>() ?? new List<ConditionItem>();
        }

        return new List<ConditionItem>();
    }

    private static bool EvaluateCondition(ConditionItem condition, NodeExecutionContext context)
    {
        var leftRaw = context.GetVariable(condition.Left);
        var rightRaw = condition.Right;

        var left = leftRaw?.ToString() ?? string.Empty;
        var right = rightRaw ?? string.Empty;

        return condition.Op?.ToLowerInvariant() switch
        {
            "eq" or "==" => left == right,
            "ne" or "!=" => left != right,
            "gt" or ">" => TryCompareNumbers(left, right) > 0,
            "gte" or ">=" => TryCompareNumbers(left, right) >= 0,
            "lt" or "<" => TryCompareNumbers(left, right) < 0,
            "lte" or "<=" => TryCompareNumbers(left, right) <= 0,
            "contains" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "not_contains" => !left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "is_empty" => string.IsNullOrEmpty(left),
            "is_not_empty" => !string.IsNullOrEmpty(left),
            _ => false
        };
    }

    private static int TryCompareNumbers(string left, string right)
    {
        if (double.TryParse(left, out var l) && double.TryParse(right, out var r))
            return l.CompareTo(r);
        return string.Compare(left, right, StringComparison.Ordinal);
    }

    private sealed class ConditionItem
    {
        public string Left { get; set; } = string.Empty;
        public string Op { get; set; } = string.Empty;
        public string? Right { get; set; }
    }
}
