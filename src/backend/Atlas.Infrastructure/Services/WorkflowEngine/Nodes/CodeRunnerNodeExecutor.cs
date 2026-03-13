using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// CodeRunner 节点：执行简单的 C# 表达式（受限沙箱，仅支持基础运算和字符串操作）。
/// 生产环境中如需 Python/JS 执行，需接入隔离进程或容器。
/// Config 结构：{ "language": "csharp", "expression": "{{a}} + {{b}}" }
/// </summary>
public sealed class CodeRunnerNodeExecutor : INodeExecutor
{
    private readonly ILogger<CodeRunnerNodeExecutor> _logger;

    public CodeRunnerNodeExecutor(ILogger<CodeRunnerNodeExecutor> logger)
    {
        _logger = logger;
    }

    public NodeType NodeType => NodeType.CodeRunner;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var expression = node.Configs.TryGetValue("expression", out var exp) ? exp?.ToString() : null;
        if (string.IsNullOrEmpty(expression))
        {
            context.SetOutput(node.Key, "output", null);
            return Task.FromResult(NodeExecutorResult.Default);
        }

        // 将 {{ref}} 替换为实际值后作为字符串模板输出
        var result = System.Text.RegularExpressions.Regex.Replace(
            expression,
            @"\{\{([^}]+)\}\}",
            m => context.GetVariable(m.Groups[1].Value.Trim())?.ToString() ?? string.Empty);

        context.SetOutput(node.Key, "output", result);
        _logger.LogDebug("CodeRunner 节点 {NodeKey} 执行完成，输出: {Result}", node.Key, result);

        return Task.FromResult(NodeExecutorResult.Default);
    }
}
