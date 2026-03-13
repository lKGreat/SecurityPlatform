using System.Text;
using Atlas.Domain.AiPlatform.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// HTTP 请求节点：发起 HTTP 调用。
/// Config 参数：url、method（GET/POST/PUT/DELETE）、headers（JSON 字符串）、body
/// 输出变量：http_status_code、http_response_body
/// </summary>
public sealed class HttpRequesterNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.HttpRequester;

    public async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var urlTemplate = context.Node.Config.GetValueOrDefault("url") ?? string.Empty;
        var method = context.Node.Config.GetValueOrDefault("method") ?? "GET";
        var bodyTemplate = context.Node.Config.GetValueOrDefault("body") ?? string.Empty;

        var url = ReplaceVariables(urlTemplate, context.Variables);
        var body = ReplaceVariables(bodyTemplate, context.Variables);

        if (string.IsNullOrWhiteSpace(url))
        {
            return new NodeExecutionResult(false, outputs, "HTTP 请求 URL 为空");
        }

        try
        {
            var factory = context.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            using var client = factory.CreateClient("WorkflowEngine");
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url);
            if (!string.IsNullOrWhiteSpace(body) && method.ToUpperInvariant() is "POST" or "PUT" or "PATCH")
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            outputs["http_status_code"] = ((int)response.StatusCode).ToString();
            outputs["http_response_body"] = responseBody;

            return new NodeExecutionResult(true, outputs);
        }
        catch (Exception ex)
        {
            return new NodeExecutionResult(false, outputs, $"HTTP 请求失败: {ex.Message}");
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
