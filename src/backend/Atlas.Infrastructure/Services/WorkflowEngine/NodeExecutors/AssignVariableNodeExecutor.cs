using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 变量赋值节点：将 config 中的 key=value 对写入变量。
/// Config 参数：assignments（格式 "key1=value1;key2=value2"）
/// </summary>
public sealed class AssignVariableNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.AssignVariable;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var assignments = context.Node.Config.GetValueOrDefault("assignments") ?? string.Empty;

        foreach (var pair in assignments.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var value = ReplaceVariables(parts[1], context.Variables);
                outputs[parts[0]] = value;
            }
        }

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }

    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
