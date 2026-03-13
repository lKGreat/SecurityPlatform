using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// OutputEmitter 节点：将文本片段通过 SSE 流式输出给前端。
/// Config 结构：{ "textRef": "llm_1.text" }
/// </summary>
public sealed class OutputEmitterNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.OutputEmitter;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var textRef = node.Configs.TryGetValue("textRef", out var tr) ? tr?.ToString() : null;
        var text = string.IsNullOrEmpty(textRef)
            ? null
            : context.GetVariable(textRef)?.ToString();

        if (text is not null)
        {
            await context.EmitEventAsync(new Application.Workflow.Models.V2.SseEvent("output_chunk", new { text }));
        }

        context.SetOutput(node.Key, "text", text);
        return NodeExecutorResult.Default;
    }
}
