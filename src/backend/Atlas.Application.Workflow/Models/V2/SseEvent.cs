namespace Atlas.Application.Workflow.Models.V2;

/// <summary>
/// SSE 推送事件帧
/// </summary>
public sealed class SseEvent
{
    public SseEvent(string eventType, object data)
    {
        EventType = eventType;
        Data = data;
    }

    /// <summary>事件类型：node_start / node_complete / node_error / workflow_done / workflow_error</summary>
    public string EventType { get; }

    public object Data { get; }
}

public sealed class NodeStartEvent
{
    public long ExecutionId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string NodeTitle { get; set; } = string.Empty;
}

public sealed class NodeCompleteEvent
{
    public long ExecutionId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long CostMs { get; set; }
    public Dictionary<string, object?> Output { get; set; } = new();
    public int? TokensUsed { get; set; }
}

public sealed class NodeErrorEvent
{
    public long ExecutionId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public long CostMs { get; set; }
}

public sealed class WorkflowDoneEvent
{
    public long ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long TotalCostMs { get; set; }
    public Dictionary<string, object?> Outputs { get; set; } = new();
}

public sealed class WorkflowInterruptEvent
{
    public long ExecutionId { get; set; }
    public string InterruptType { get; set; } = string.Empty;
    public string NodeKey { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
}
