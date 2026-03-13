using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// LLM 节点：调用大模型，支持 prompt 模板变量替换。
/// Config 结构：
/// {
///   "model": "gpt-4o",
///   "provider": "openai",
///   "temperature": 0.7,
///   "maxTokens": 2000,
///   "systemPrompt": "...",
///   "userPrompt": "你好，{{entry_1.userInput}}"
/// }
/// </summary>
public sealed class LlmNodeExecutor : INodeExecutor
{
    private readonly ILlmProviderFactory _llmProviderFactory;

    public LlmNodeExecutor(ILlmProviderFactory llmProviderFactory)
    {
        _llmProviderFactory = llmProviderFactory;
    }

    public NodeType NodeType => NodeType.LLM;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var model = GetConfigString(node, "model") ?? "gpt-4o-mini";
        var provider = GetConfigString(node, "provider");
        var systemPrompt = ResolveTemplate(GetConfigString(node, "systemPrompt"), node, context);
        var userPrompt = ResolveTemplate(GetConfigString(node, "userPrompt") ?? GetConfigString(node, "prompt"), node, context);
        var temperature = GetConfigFloat(node, "temperature");
        var maxTokens = GetConfigInt(node, "maxTokens");

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            context.SetOutput(node.Key, "text", string.Empty);
            context.SetOutput(node.Key, "tokensUsed", 0);
            return NodeExecutorResult.Default;
        }

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new ChatMessage("system", systemPrompt));
        messages.Add(new ChatMessage("user", userPrompt));

        var llm = _llmProviderFactory.GetLlmProvider(provider);
        var request = new ChatCompletionRequest(model, messages, temperature, maxTokens, provider);
        var result = await llm.ChatAsync(request, context.CancellationToken);

        context.SetOutput(node.Key, "text", result.Content);
        context.SetOutput(node.Key, "tokensUsed", result.TotalTokens);

        return NodeExecutorResult.Default;
    }

    private static string? GetConfigString(NodeSchema node, string key)
    {
        return node.Configs.TryGetValue(key, out var v) ? v?.ToString() : null;
    }

    private static float? GetConfigFloat(NodeSchema node, string key)
    {
        if (node.Configs.TryGetValue(key, out var v) && v is not null &&
            float.TryParse(v.ToString(), out var f))
            return f;
        return null;
    }

    private static int? GetConfigInt(NodeSchema node, string key)
    {
        if (node.Configs.TryGetValue(key, out var v) && v is not null &&
            int.TryParse(v.ToString(), out var i))
            return i;
        return null;
    }

    private static string? ResolveTemplate(string? template, NodeSchema node, NodeExecutionContext context)
    {
        if (string.IsNullOrEmpty(template)) return template;

        // 替换 {{nodeKey.fieldName}} 占位符
        var result = System.Text.RegularExpressions.Regex.Replace(
            template,
            @"\{\{([^}]+)\}\}",
            m =>
            {
                var key = m.Groups[1].Value.Trim();
                return context.GetVariable(key)?.ToString() ?? string.Empty;
            });

        return result;
    }
}
