using System.Text.Json;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// IntentDetector 节点：基于 LLM 对用户输入进行意图分类。
/// Config 结构：
/// {
///   "model": "gpt-4o-mini",
///   "inputRef": "entry_1.userInput",
///   "intents": ["question", "complaint", "feedback", "other"]
/// }
/// </summary>
public sealed class IntentDetectorNodeExecutor : INodeExecutor
{
    private readonly ILlmProviderFactory _llmFactory;

    public IntentDetectorNodeExecutor(ILlmProviderFactory llmFactory)
    {
        _llmFactory = llmFactory;
    }

    public NodeType NodeType => NodeType.IntentDetector;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var model = node.Configs.TryGetValue("model", out var m) ? m?.ToString() ?? "gpt-4o-mini" : "gpt-4o-mini";
        var inputRef = node.Configs.TryGetValue("inputRef", out var ir) ? ir?.ToString() : null;
        var input = string.IsNullOrEmpty(inputRef) ? string.Empty : context.GetVariable(inputRef)?.ToString() ?? string.Empty;

        List<string> intents = new();
        if (node.Configs.TryGetValue("intents", out var intentsObj) &&
            intentsObj is System.Text.Json.JsonElement je &&
            je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            intents = je.Deserialize<List<string>>() ?? intents;
        }

        if (intents.Count == 0 || string.IsNullOrEmpty(input))
        {
            context.SetOutput(node.Key, "intent", "other");
            context.SetOutput(node.Key, "confidence", 0.0);
            return NodeExecutorResult.Default;
        }

        var intentsStr = string.Join(", ", intents.Select(i => $"\"{i}\""));
        var prompt = $"请将以下用户输入分类为这些意图之一：{intentsStr}。\n\n用户输入：{input}\n\n只回复意图名称，不要解释。";

        var llm = _llmFactory.GetLlmProvider();
        var result = await llm.ChatAsync(
            new ChatCompletionRequest(model, [new ChatMessage("user", prompt)], 0.0f, 50),
            context.CancellationToken);

        var detectedIntent = result.Content?.Trim().Trim('"') ?? "other";
        if (!intents.Contains(detectedIntent, StringComparer.OrdinalIgnoreCase))
            detectedIntent = "other";

        context.SetOutput(node.Key, "intent", detectedIntent);
        context.SetOutput(node.Key, "confidence", 0.9);

        return NodeExecutorResult.Port(detectedIntent);
    }
}
