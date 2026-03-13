using System.Threading.Channels;
using Atlas.Application.AiPlatform.Models;
using Atlas.Domain.AiPlatform.Enums;
using Atlas.Domain.AiPlatform.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// V2 工作流节点执行器接口。每种 <see cref="WorkflowNodeType"/> 对应一个实现。
/// </summary>
public interface INodeExecutor
{
    WorkflowNodeType NodeType { get; }

    Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// 节点执行上下文——由 DagExecutor 为每个节点创建。
/// </summary>
public sealed class NodeExecutionContext
{
    public NodeExecutionContext(
        NodeSchema node,
        Dictionary<string, string> variables,
        IServiceProvider serviceProvider,
        long executionId,
        Channel<SseEvent>? eventChannel)
    {
        Node = node;
        Variables = variables;
        ServiceProvider = serviceProvider;
        ExecutionId = executionId;
        EventChannel = eventChannel;
    }

    public NodeSchema Node { get; }
    public Dictionary<string, string> Variables { get; }
    public IServiceProvider ServiceProvider { get; }
    public long ExecutionId { get; }
    public Channel<SseEvent>? EventChannel { get; }

    /// <summary>
    /// 向 SSE 事件通道写入一条事件（如果通道可用）。
    /// </summary>
    public async ValueTask EmitEventAsync(string eventType, string data, CancellationToken cancellationToken)
    {
        if (EventChannel is not null)
        {
            await EventChannel.Writer.WriteAsync(new SseEvent(eventType, data), cancellationToken);
        }
    }

    /// <summary>
    /// 将模板中的 {{key}} 占位符替换为变量值（忽略大小写）。
    /// </summary>
    public string ReplaceVariables(string template)
    {
        if (string.IsNullOrEmpty(template) || Variables.Count == 0)
        {
            return template;
        }

        var result = template;
        foreach (var kvp in Variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}

/// <summary>
/// 节点执行结果。
/// </summary>
public sealed record NodeExecutionResult(
    bool Success,
    Dictionary<string, string> Outputs,
    string? ErrorMessage = null,
    InterruptType InterruptType = InterruptType.None);

/// <summary>
/// 节点类型元数据——供 NodeExecutorRegistry 对外暴露。
/// </summary>
public sealed record NodeTypeMetadata(
    WorkflowNodeType Type,
    string Key,
    string Name,
    string Category,
    string Description);
