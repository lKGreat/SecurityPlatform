using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Domain.AiPlatform.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// LLM 调用节点：通过 ILlmProviderFactory 调用大语言模型。
/// Config 参数：prompt, model, provider, temperature, maxTokens, outputKey
/// </summary>
public sealed class LlmNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Llm;

    public async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var config = context.Node.Config;

        var promptTemplate = config.GetValueOrDefault("prompt") ?? string.Empty;
        var model = config.GetValueOrDefault("model") ?? "gpt-4o-mini";
        var provider = config.GetValueOrDefault("provider");
        var outputKey = config.GetValueOrDefault("outputKey") ?? "llm_output";

        // 变量替换
        var prompt = ReplaceVariables(promptTemplate, context.Variables);
        if (string.IsNullOrWhiteSpace(prompt))
        {
            outputs[outputKey] = string.Empty;
            return new NodeExecutionResult(true, outputs);
        }

        try
        {
            var factory = context.ServiceProvider.GetRequiredService<ILlmProviderFactory>();
            var llmProvider = factory.GetLlmProvider(provider);

            float.TryParse(config.GetValueOrDefault("temperature"), out var temperature);
            int.TryParse(config.GetValueOrDefault("maxTokens"), out var maxTokens);

            var request = new ChatCompletionRequest(
                model,
                [new ChatMessage("user", prompt)],
                temperature > 0 ? temperature : null,
                maxTokens > 0 ? maxTokens : null,
                provider);

            var result = await llmProvider.ChatAsync(request, cancellationToken);
            outputs[outputKey] = result.Content;

            await context.EmitEventAsync("llm_output", result.Content, cancellationToken);

            return new NodeExecutionResult(true, outputs);
        }
        catch (Exception ex)
        {
            return new NodeExecutionResult(false, outputs, $"LLM 调用失败: {ex.Message}");
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
