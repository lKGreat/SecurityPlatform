using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using SqlSugar;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// ConversationList 节点：列出指定 Agent 或用户的会话列表。
/// Config 结构：{ "agentIdRef": "entry_1.agentId", "limit": 10 }
/// </summary>
public sealed class ConversationListNodeExecutor : INodeExecutor
{
    private readonly ISqlSugarClient _db;

    public ConversationListNodeExecutor(ISqlSugarClient db)
    {
        _db = db;
    }

    public NodeType NodeType => NodeType.ConversationList;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var agentIdRef = node.Configs.TryGetValue("agentIdRef", out var ar) ? ar?.ToString() : null;
        var agentId = string.IsNullOrEmpty(agentIdRef) ? null : context.GetVariable(agentIdRef)?.ToString();

        var limit = 10;
        if (node.Configs.TryGetValue("limit", out var lv) && int.TryParse(lv?.ToString(), out var parsedLimit))
            limit = parsedLimit;

        List<dynamic> conversations;
        if (!string.IsNullOrEmpty(agentId) && long.TryParse(agentId, out var agentIdLong))
        {
            conversations = (await _db.Ado.SqlQueryAsync<dynamic>(
                $"SELECT * FROM conversations WHERE agent_id = {agentIdLong} ORDER BY created_at DESC LIMIT {limit}"))?.ToList() ?? new List<dynamic>();
        }
        else
        {
            conversations = new List<dynamic>();
        }

        context.SetOutput(node.Key, "conversations", conversations);
        context.SetOutput(node.Key, "count", conversations.Count);

        return NodeExecutorResult.Default;
    }
}
