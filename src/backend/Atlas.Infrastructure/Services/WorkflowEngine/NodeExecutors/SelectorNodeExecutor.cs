using System.Text.Json;
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
        var outputs = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var condition = context.GetConfigString("condition");

        var result = context.EvaluateCondition(condition);
        outputs["selector_result"] = JsonSerializer.SerializeToElement(result);
        outputs["selected_branch"] = VariableResolver.CreateStringElement(result ? "true_branch" : "false_branch");

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
