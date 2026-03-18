using System.Text.Json;
using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 循环节点：支持计数循环。
/// Config 参数：maxIterations（默认 10）
/// 输出变量：loop_index、loop_completed
/// </summary>
public sealed class LoopNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.Loop;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var maxIterations = context.GetConfigInt32("maxIterations", 10);
        if (maxIterations <= 0)
        {
            maxIterations = 10;
        }

        var currentIndex = 0;
        if (context.Variables.TryGetValue("loop_index", out var loopIndexValue))
        {
            if (loopIndexValue.ValueKind == JsonValueKind.Number)
            {
                _ = loopIndexValue.TryGetInt32(out currentIndex);
            }
            else
            {
                var loopIndexText = VariableResolver.ToDisplayText(loopIndexValue);
                _ = int.TryParse(loopIndexText, out currentIndex);
            }
        }

        var outputs = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (currentIndex < maxIterations)
        {
            outputs["loop_index"] = JsonSerializer.SerializeToElement(currentIndex + 1);
            outputs["loop_completed"] = JsonSerializer.SerializeToElement(false);
        }
        else
        {
            outputs["loop_index"] = JsonSerializer.SerializeToElement(currentIndex);
            outputs["loop_completed"] = JsonSerializer.SerializeToElement(true);
        }

        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
