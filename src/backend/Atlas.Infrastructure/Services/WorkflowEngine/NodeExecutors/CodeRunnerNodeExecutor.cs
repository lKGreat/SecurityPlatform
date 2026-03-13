using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 代码执行节点：当前版本支持简单表达式求值（字符串拼接、变量替换）。
/// 安全沙箱执行（Roslyn Scripting）将在后续版本中实现。
/// Config 参数：code、language（默认 "expression"）、outputKey
/// </summary>
public sealed class CodeRunnerNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.CodeRunner;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var code = context.Node.Config.GetValueOrDefault("code") ?? string.Empty;
        var outputKey = context.Node.Config.GetValueOrDefault("outputKey") ?? "code_output";
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // 简单表达式求值：支持变量替换
            var result = ReplaceVariables(code, context.Variables);
            outputs[outputKey] = result;
            return Task.FromResult(new NodeExecutionResult(true, outputs));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new NodeExecutionResult(false, outputs, $"代码执行失败: {ex.Message}"));
        }
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
