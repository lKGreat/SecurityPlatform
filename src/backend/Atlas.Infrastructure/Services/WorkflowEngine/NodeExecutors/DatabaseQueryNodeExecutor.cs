using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine.NodeExecutors;

/// <summary>
/// 数据库查询节点：当前版本为占位实现。
/// Config 参数：query、databaseId、outputKey
/// </summary>
public sealed class DatabaseQueryNodeExecutor : INodeExecutor
{
    public WorkflowNodeType NodeType => WorkflowNodeType.DatabaseQuery;

    public Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken)
    {
        var outputKey = context.Node.Config.GetValueOrDefault("outputKey") ?? "query_result";
        var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [outputKey] = "[]"
        };

        // 占位：TODO[coze-v2-db-query] 集成 AiDatabase 数据源执行查询
        return Task.FromResult(new NodeExecutionResult(true, outputs));
    }
}
