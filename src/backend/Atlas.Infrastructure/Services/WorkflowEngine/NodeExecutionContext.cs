using System.Threading.Channels;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// 单个节点执行时的上下文，持有数据 Bag 和事件通道。
/// </summary>
public sealed class NodeExecutionContext
{
    private readonly Dictionary<string, object?> _dataBag;

    public NodeExecutionContext(
        long executionId,
        CanvasSchema canvas,
        Dictionary<string, object?> initialInputs,
        Channel<SseEvent>? sseChannel,
        CancellationToken cancellationToken)
    {
        ExecutionId = executionId;
        Canvas = canvas;
        _dataBag = new Dictionary<string, object?>(initialInputs);
        SseChannel = sseChannel;
        CancellationToken = cancellationToken;
    }

    public long ExecutionId { get; }

    public CanvasSchema Canvas { get; }

    /// <summary>SSE 事件通道（null 表示非流式执行）</summary>
    public Channel<SseEvent>? SseChannel { get; }

    public CancellationToken CancellationToken { get; }

    /// <summary>节点执行中断类型（由节点设置，DAG 引擎读取）</summary>
    public InterruptType InterruptType { get; private set; }

    public string? InterruptContextJson { get; private set; }

    /// <summary>
    /// 读取变量，格式："{nodeKey}.{outputField}"，或纯字面量（前缀 "literal:"）。
    /// </summary>
    public object? GetVariable(string reference)
    {
        if (reference.StartsWith("literal:", StringComparison.OrdinalIgnoreCase))
            return reference["literal:".Length..];

        return _dataBag.TryGetValue(reference, out var val) ? val : null;
    }

    /// <summary>
    /// 写入节点输出变量，key = "{nodeKey}.{outputField}"。
    /// </summary>
    public void SetOutput(string nodeKey, string field, object? value)
    {
        _dataBag[$"{nodeKey}.{field}"] = value;
    }

    /// <summary>批量写入节点输出</summary>
    public void SetOutputs(string nodeKey, Dictionary<string, object?> outputs)
    {
        foreach (var (field, value) in outputs)
        {
            _dataBag[$"{nodeKey}.{field}"] = value;
        }
    }

    /// <summary>获取当前所有数据（用于 Exit 节点收集最终输出）</summary>
    public IReadOnlyDictionary<string, object?> GetAllData() => _dataBag;

    public void SetInterrupt(InterruptType type, string contextJson)
    {
        InterruptType = type;
        InterruptContextJson = contextJson;
    }

    public async ValueTask EmitEventAsync(SseEvent evt)
    {
        if (SseChannel is not null)
        {
            await SseChannel.Writer.WriteAsync(evt, CancellationToken);
        }
    }
}
