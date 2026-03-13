using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using SqlSugar;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// MessageList 节点：读取指定会话的消息列表（直接查询 chat_messages 表）。
/// Config 结构：{ "conversationIdRef": "entry_1.conversationId", "limit": 20 }
/// </summary>
public sealed class MessageListNodeExecutor : INodeExecutor
{
    private readonly ISqlSugarClient _db;

    public MessageListNodeExecutor(ISqlSugarClient db)
    {
        _db = db;
    }

    public NodeType NodeType => NodeType.MessageList;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var convIdRef = node.Configs.TryGetValue("conversationIdRef", out var cr) ? cr?.ToString() : null;
        var conversationId = string.IsNullOrEmpty(convIdRef) ? null : context.GetVariable(convIdRef)?.ToString();

        if (string.IsNullOrEmpty(conversationId) || !long.TryParse(conversationId, out var convIdLong))
        {
            context.SetOutput(node.Key, "messages", Array.Empty<object>());
            context.SetOutput(node.Key, "count", 0);
            return NodeExecutorResult.Default;
        }

        var limit = 20;
        if (node.Configs.TryGetValue("limit", out var lv) && int.TryParse(lv?.ToString(), out var parsedLimit))
            limit = parsedLimit;

        var messages = await _db.Ado.SqlQueryAsync<dynamic>(
            $"SELECT * FROM chat_messages WHERE conversation_id = {convIdLong} ORDER BY created_at DESC LIMIT {limit}");

        var list = messages?.ToList() ?? new List<dynamic>();

        context.SetOutput(node.Key, "messages", list);
        context.SetOutput(node.Key, "count", list.Count);

        return NodeExecutorResult.Default;
    }
}
