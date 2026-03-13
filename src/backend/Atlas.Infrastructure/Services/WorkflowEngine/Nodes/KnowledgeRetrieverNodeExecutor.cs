using System.Text.Json;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// KnowledgeRetriever 节点：从知识库检索相关文档片段。
/// Config 结构：
/// {
///   "queryRef": "entry_1.query",
///   "knowledgeBaseIds": [123, 456],
///   "topK": 5
/// }
/// </summary>
public sealed class KnowledgeRetrieverNodeExecutor : INodeExecutor
{
    private readonly IRagRetrievalService _ragRetrievalService;
    private readonly ITenantProvider _tenantProvider;

    public KnowledgeRetrieverNodeExecutor(IRagRetrievalService ragRetrievalService, ITenantProvider tenantProvider)
    {
        _ragRetrievalService = ragRetrievalService;
        _tenantProvider = tenantProvider;
    }

    public NodeType NodeType => NodeType.KnowledgeRetriever;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var queryRef = node.Configs.TryGetValue("queryRef", out var qr) ? qr?.ToString() : null;
        var query = string.IsNullOrEmpty(queryRef) ? string.Empty : context.GetVariable(queryRef)?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            context.SetOutput(node.Key, "results", Array.Empty<object>());
            context.SetOutput(node.Key, "count", 0);
            return NodeExecutorResult.Default;
        }

        var topK = 5;
        if (node.Configs.TryGetValue("topK", out var tk) && int.TryParse(tk?.ToString(), out var parsedTopK))
            topK = parsedTopK;

        var kbIds = new List<long>();
        if (node.Configs.TryGetValue("knowledgeBaseIds", out var kbIdsObj) &&
            kbIdsObj is System.Text.Json.JsonElement je &&
            je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            kbIds = je.Deserialize<List<long>>() ?? kbIds;
        }

        var tenantId = _tenantProvider.GetTenantId();
        var results = await _ragRetrievalService.SearchAsync(tenantId, kbIds, query, topK, context.CancellationToken);

        context.SetOutput(node.Key, "results", results);
        context.SetOutput(node.Key, "count", results?.Count() ?? 0);

        return NodeExecutorResult.Default;
    }
}
