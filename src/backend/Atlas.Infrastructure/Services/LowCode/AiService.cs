using Atlas.Application.LowCode.Abstractions;
using Atlas.Application.LowCode.Models;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Core.Tenancy;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Atlas.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Infrastructure.Services.LowCode;

/// <summary>
/// AI 辅助开发服务实现（基于 Provider 抽象，支持 OpenAI / DeepSeek / Ollama）
/// </summary>
public sealed class AiService : IAiService
{
    private readonly ILlmProviderFactory _llmProviderFactory;
    private readonly ILogger<AiService> _logger;
    private readonly AiPlatformOptions _aiOptions;

    public AiService(
        ILlmProviderFactory llmProviderFactory,
        IOptions<AiPlatformOptions> aiOptions,
        ILogger<AiService> logger)
    {
        _llmProviderFactory = llmProviderFactory;
        _logger = logger;
        _aiOptions = aiOptions.Value;
    }

    public async Task<AiFormGenerateResponse> GenerateFormAsync(
        TenantId tenantId, AiFormGenerateRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"请根据以下描述生成 amis JSON Schema 表单定义：\n\n{request.Description}";
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            prompt += $"\n\n分类：{request.Category}";
        }

        var schemaJson = await CallAiAsync(prompt, cancellationToken);

        // Try to extract JSON from the response
        var jsonStart = schemaJson.IndexOf('{');
        var jsonEnd = schemaJson.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            schemaJson = schemaJson[jsonStart..(jsonEnd + 1)];
        }
        else
        {
            // Generate a basic schema as fallback
            schemaJson = GenerateBasicFormSchema(request.Description);
        }

        return new AiFormGenerateResponse(schemaJson, "AI 已根据描述生成表单 Schema");
    }

    public async Task<AiSqlGenerateResponse> GenerateSqlAsync(
        TenantId tenantId, AiSqlGenerateRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"请将以下自然语言查询转换为 SQL：\n\n{request.Question}";
        if (!string.IsNullOrWhiteSpace(request.TableContext))
        {
            prompt += $"\n\n数据库表结构：\n{request.TableContext}";
        }

        var sql = await CallAiAsync(prompt, cancellationToken);
        return new AiSqlGenerateResponse(sql, "AI 已根据描述生成 SQL");
    }

    public async Task<AiWorkflowSuggestResponse> SuggestWorkflowAsync(
        TenantId tenantId, AiWorkflowSuggestRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"请根据以下业务流程描述生成 BPMN 工作流定义 JSON：\n\n{request.Description}";
        var definitionJson = await CallAiAsync(prompt, cancellationToken);
        return new AiWorkflowSuggestResponse(definitionJson, "AI 已根据描述建议工作流");
    }

    public async Task<AiChatResponse> ChatAsync(
        TenantId tenantId, AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = request.Message;
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            prompt = $"上下文：{request.Context}\n\n用户问题：{request.Message}";
        }

        var reply = await CallAiAsync(prompt, cancellationToken);
        return new AiChatResponse(reply);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        TenantId tenantId, AiChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = request.Message;
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            prompt = $"上下文：{request.Context}\n\n用户问题：{request.Message}";
        }

        var requestModel = BuildChatRequest(prompt);
        ChatCompletionResult? fallbackResult = null;
        IAsyncEnumerable<ChatCompletionChunk>? stream = null;

        try
        {
            var provider = _llmProviderFactory.GetLlmProvider(requestModel.Provider);
            stream = provider.ChatStreamAsync(requestModel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI stream provider unavailable, fallback to template mode.");
            fallbackResult = await GetTemplateFallbackAsync(prompt);
        }

        if (stream is not null)
        {
            await using var streamEnumerator = stream.GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                ChatCompletionChunk chunk;
                try
                {
                    if (!await streamEnumerator.MoveNextAsync())
                    {
                        break;
                    }

                    chunk = streamEnumerator.Current;
                }

                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI stream execution failed, fallback to template mode.");
                    fallbackResult = await GetTemplateFallbackAsync(prompt);
                    break;
                }

                if (!string.IsNullOrWhiteSpace(chunk.ContentDelta))
                {
                    yield return chunk.ContentDelta;
                }
            }
        }

        if (fallbackResult is not null)
        {
            yield return fallbackResult.Content;
        }
    }

    /// <summary>
    /// Calls the configured AI provider. Falls back to a template-based response
    /// when no AI provider is configured.
    /// </summary>
    private async Task<string> CallAiAsync(string prompt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI prompt: {Prompt}", prompt.Length > 200 ? prompt[..200] + "..." : prompt);

        try
        {
            var request = BuildChatRequest(prompt);
            var provider = _llmProviderFactory.GetLlmProvider(request.Provider);
            var result = await provider.ChatAsync(request, cancellationToken);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI provider call failed. Fallback to template response.");
            var fallback = await GetTemplateFallbackAsync(prompt);
            return fallback.Content;
        }
    }

    private ChatCompletionRequest BuildChatRequest(string prompt)
    {
        var provider = _aiOptions.DefaultProvider;
        var model = _aiOptions.Providers.TryGetValue(provider, out var providerOption)
                    && !string.IsNullOrWhiteSpace(providerOption.DefaultModel)
            ? providerOption.DefaultModel
            : "gpt-4o-mini";

        return new ChatCompletionRequest(
            model,
            [new ChatMessage("user", prompt)],
            Temperature: 0.2f,
            MaxTokens: 2048,
            Provider: provider);
    }

    private async Task<ChatCompletionResult> GetTemplateFallbackAsync(string prompt)
    {
        await Task.CompletedTask;

        if (prompt.Contains("表单", StringComparison.OrdinalIgnoreCase) || prompt.Contains("form", StringComparison.OrdinalIgnoreCase))
        {
            return new ChatCompletionResult(GenerateBasicFormSchema(prompt), Provider: "fallback");
        }

        if (prompt.Contains("SQL", StringComparison.OrdinalIgnoreCase) || prompt.Contains("查询", StringComparison.OrdinalIgnoreCase))
        {
            return new ChatCompletionResult("SELECT * FROM table_name WHERE 1=1 LIMIT 100;", Provider: "fallback");
        }

        if (prompt.Contains("工作流", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("workflow", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("BPMN", StringComparison.OrdinalIgnoreCase))
        {
            return new ChatCompletionResult(
                JsonSerializer.Serialize(new
                {
                    nodes = new[]
                    {
                        new { id = "start", type = "StartEvent", name = "开始" },
                        new { id = "task1", type = "UserTask", name = "审批节点" },
                        new { id = "end", type = "EndEvent", name = "结束" }
                    },
                    edges = new[]
                    {
                        new { source = "start", target = "task1" },
                        new { source = "task1", target = "end" }
                    }
                }),
                Provider: "fallback");
        }

        return new ChatCompletionResult(
            "AI 服务已收到请求。请配置 AI 提供商（OpenAI / DeepSeek / Ollama）以启用完整功能。",
            Provider: "fallback");
    }

    private static string GenerateBasicFormSchema(string description)
    {
        return JsonSerializer.Serialize(new
        {
            type = "page",
            title = "AI 生成表单",
            body = new object[]
            {
                new
                {
                    type = "form",
                    title = "",
                    body = new object[]
                    {
                        new { type = "input-text", name = "name", label = "名称", required = true },
                        new { type = "textarea", name = "description", label = "描述" },
                        new { type = "input-date", name = "date", label = "日期" },
                        new { type = "select", name = "status", label = "状态", options = new[]
                        {
                            new { label = "草稿", value = "draft" },
                            new { label = "已提交", value = "submitted" },
                            new { label = "已完成", value = "completed" }
                        }}
                    }
                }
            }
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
