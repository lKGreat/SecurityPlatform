using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 条件分支节点：根据 config 中的 "condition" 表达式与变量做简单字符串匹配。
/// 当前仅支持 "variable == value" 形式的简单比较。
/// </summary>
public sealed class SelectorNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Selector;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var condition = context.Node.Config.GetValueOrDefault("condition") ?? string.Empty;

        // 简单求值：支持 "variableName == expectedValue"
        var result = EvaluateCondition(condition, context.Variables);
        outputs["selector_result"] = result ? "true" : "false";
        outputs["selected_branch"] = result ? "true_branch" : "false_branch";

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }

    private static bool EvaluateCondition(string condition, Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        // 支持 "key == value" 和 "key != value"
        var eqParts = condition.Split("==", 2, StringSplitOptions.TrimEntries);
        if (eqParts.Length == 2)
        {
            var actual = variables.GetValueOrDefault(eqParts[0]) ?? string.Empty;
            return string.Equals(actual, eqParts[1], StringComparison.OrdinalIgnoreCase);
        }

        var neParts = condition.Split("!=", 2, StringSplitOptions.TrimEntries);
        if (neParts.Length == 2)
        {
            var actual = variables.GetValueOrDefault(neParts[0]) ?? string.Empty;
            return !string.Equals(actual, neParts[1], StringComparison.OrdinalIgnoreCase);
        }

        // 默认：检查变量是否有值
        return !string.IsNullOrWhiteSpace(variables.GetValueOrDefault(condition));
    }
}
