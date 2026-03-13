using System.Text.Json;
using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// JSON 反序列化节点：将 JSON 字符串反序列化为变量。
/// Config 参数：inputVariable（变量名，其值为 JSON 字符串）
/// </summary>
public sealed class JsonDeserializationNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.JsonDeserialization;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var inputVariable = context.Node.Config.GetValueOrDefault("inputVariable") ?? string.Empty;
        var jsonStr = context.Variables.GetValueOrDefault(inputVariable) ?? "{}";
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonStr);
            if (parsed is not null)
            {
                foreach (var kvp in parsed)
                {
                    outputs[kvp.Key] = kvp.Value.ToString();
                }
            }

            return Task.FromResult(new NodeExecutionResult(true, outputs));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new NodeExecutionResult(false, outputs, $"JSON 反序列化失败: {ex.Message}"));
        }
    }
}
