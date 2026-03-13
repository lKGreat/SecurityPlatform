using System.Text;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// HttpRequester 节点：发起 HTTP 请求。
/// Config 结构：
/// {
///   "url": "https://api.example.com/endpoint",
///   "method": "POST",
///   "headers": { "Content-Type": "application/json" },
///   "bodyTemplate": "{\"query\": \"{{entry_1.query}}\"}"
/// }
/// </summary>
public sealed class HttpRequesterNodeExecutor : INodeExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpRequesterNodeExecutor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public NodeType NodeType => NodeType.HttpRequester;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var url = ResolveTemplate(GetConfigString(node, "url"), node, context)
            ?? throw new InvalidOperationException($"HttpRequester 节点 {node.Key} 缺少 url 配置");

        var method = GetConfigString(node, "method")?.ToUpperInvariant() ?? "GET";
        var bodyTemplate = ResolveTemplate(GetConfigString(node, "bodyTemplate"), node, context);
        var headersJson = node.Configs.TryGetValue("headers", out var hv) ? hv : null;

        var client = _httpClientFactory.CreateClient("WorkflowHttp");
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (headersJson is System.Text.Json.JsonElement headerEl && headerEl.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var prop in headerEl.EnumerateObject())
            {
                request.Headers.TryAddWithoutValidation(prop.Name, prop.Value.GetString());
            }
        }

        if (!string.IsNullOrEmpty(bodyTemplate))
        {
            request.Content = new StringContent(bodyTemplate, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, context.CancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(context.CancellationToken);

        context.SetOutput(node.Key, "statusCode", (int)response.StatusCode);
        context.SetOutput(node.Key, "body", responseBody);
        context.SetOutput(node.Key, "success", response.IsSuccessStatusCode);

        return NodeExecutorResult.Default;
    }

    private static string? GetConfigString(NodeSchema node, string key)
    {
        return node.Configs.TryGetValue(key, out var v) ? v?.ToString() : null;
    }

    private static string? ResolveTemplate(string? template, NodeSchema node, NodeExecutionContext context)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return System.Text.RegularExpressions.Regex.Replace(
            template,
            @"\{\{([^}]+)\}\}",
            m => context.GetVariable(m.Groups[1].Value.Trim())?.ToString() ?? string.Empty);
    }
}
