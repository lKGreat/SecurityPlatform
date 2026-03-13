using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using SqlSugar;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// DatabaseQuery 节点：执行 SQL 查询并返回结果。
/// Config 结构：
/// {
///   "sql": "SELECT * FROM assets WHERE name = '{{entry_1.name}}'",
///   "parameters": { "name": "entry_1.name" }
/// }
/// </summary>
public sealed class DatabaseQueryNodeExecutor : INodeExecutor
{
    private readonly ISqlSugarClient _db;

    public DatabaseQueryNodeExecutor(ISqlSugarClient db)
    {
        _db = db;
    }

    public NodeType NodeType => NodeType.DatabaseQuery;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var sql = GetResolvedSql(node, context);
        if (string.IsNullOrWhiteSpace(sql))
        {
            context.SetOutput(node.Key, "rows", Array.Empty<object>());
            context.SetOutput(node.Key, "count", 0);
            return NodeExecutorResult.Default;
        }

        var rows = await _db.Ado.SqlQueryAsync<dynamic>(sql);
        var list = rows?.ToList() ?? new List<dynamic>();

        context.SetOutput(node.Key, "rows", list);
        context.SetOutput(node.Key, "count", list.Count);
        context.SetOutput(node.Key, "firstRow", list.FirstOrDefault());

        return NodeExecutorResult.Default;
    }

    private static string GetResolvedSql(NodeSchema node, NodeExecutionContext context)
    {
        var sql = node.Configs.TryGetValue("sql", out var s) ? s?.ToString() ?? "" : "";
        return System.Text.RegularExpressions.Regex.Replace(
            sql,
            @"\{\{([^}]+)\}\}",
            m => context.GetVariable(m.Groups[1].Value.Trim())?.ToString()?.Replace("'", "''") ?? string.Empty);
    }
}
