using System.Text.Json;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// QuestionAnswer 节点：向用户提问并等待回答（中断节点）。
/// Config 结构：{ "promptText": "请输入您的需求：" }
/// 执行时抛出 WorkflowInterruptException，引擎会挂起执行并等待 Resume。
/// </summary>
public sealed class QuestionAnswerNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.QuestionAnswer;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var promptText = node.Configs.TryGetValue("promptText", out var pt) ? pt?.ToString() ?? "" : "";

        // 通知前端需要用户输入
        await context.EmitEventAsync(new SseEvent("workflow_interrupt", new WorkflowInterruptEvent
        {
            ExecutionId = context.ExecutionId,
            InterruptType = "Question",
            NodeKey = node.Key,
            PromptText = promptText
        }));

        var contextJson = JsonSerializer.Serialize(new { nodeKey = node.Key, promptText });
        context.SetInterrupt(InterruptType.Question, contextJson);

        throw new WorkflowInterruptException(InterruptType.Question, contextJson);
    }
}
