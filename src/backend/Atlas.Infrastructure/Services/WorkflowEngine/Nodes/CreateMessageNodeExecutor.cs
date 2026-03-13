using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using SqlSugar;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// CreateMessage 节点：向指定会话添加消息。
/// Config 结构：
/// {
///   "conversationIdRef": "entry_1.conversationId",
///   "role": "assistant",
///   "contentRef": "llm_1.text"
/// }
/// </summary>
public sealed class CreateMessageNodeExecutor : INodeExecutor
{
    private readonly ISqlSugarClient _db;

    public CreateMessageNodeExecutor(ISqlSugarClient db)
    {
        _db = db;
    }

    public NodeType NodeType => NodeType.CreateMessage;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var convIdRef = node.Configs.TryGetValue("conversationIdRef", out var cr) ? cr?.ToString() : null;
        var conversationId = string.IsNullOrEmpty(convIdRef) ? null : context.GetVariable(convIdRef)?.ToString();

        var role = node.Configs.TryGetValue("role", out var rv) ? rv?.ToString() ?? "assistant" : "assistant";
        var contentRef = node.Configs.TryGetValue("contentRef", out var ctv) ? ctv?.ToString() : null;
        var content = string.IsNullOrEmpty(contentRef) ? string.Empty : context.GetVariable(contentRef)?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(conversationId) || !long.TryParse(conversationId, out var convIdLong))
        {
            context.SetOutput(node.Key, "messageId", null);
            context.SetOutput(node.Key, "success", false);
            return NodeExecutorResult.Default;
        }

        var messageId = await _db.Ado.ExecuteCommandAsync(
            $"INSERT INTO chat_messages (conversation_id, role, content, created_at) VALUES ({convIdLong}, '{role.Replace("'", "''")}', '{content.Replace("'", "''")}', datetime('now'))");

        context.SetOutput(node.Key, "messageId", messageId);
        context.SetOutput(node.Key, "success", messageId > 0);
        context.SetOutput(node.Key, "content", content);

        return NodeExecutorResult.Default;
    }
}
