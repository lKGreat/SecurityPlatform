using System.Text.Json;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// SubWorkflow 节点：调用另一个已发布的工作流作为子流程。
/// Config 结构：{ "workflowId": 123, "version": "1.0.0" }
/// </summary>
public sealed class SubWorkflowNodeExecutor : INodeExecutor
{
    private readonly IWorkflowMetaRepository _metaRepo;
    private readonly IWorkflowVersionRepository _versionRepo;
    private readonly IWorkflowDraftRepository _draftRepo;
    private readonly DagExecutor _dagExecutor;
    private readonly ITenantProvider _tenantProvider;

    public SubWorkflowNodeExecutor(
        IWorkflowMetaRepository metaRepo,
        IWorkflowVersionRepository versionRepo,
        IWorkflowDraftRepository draftRepo,
        DagExecutor dagExecutor,
        ITenantProvider tenantProvider)
    {
        _metaRepo = metaRepo;
        _versionRepo = versionRepo;
        _draftRepo = draftRepo;
        _dagExecutor = dagExecutor;
        _tenantProvider = tenantProvider;
    }

    public NodeType NodeType => NodeType.SubWorkflow;

    public async Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var workflowIdStr = node.Configs.TryGetValue("workflowId", out var wid) ? wid?.ToString() : null;
        if (!long.TryParse(workflowIdStr, out var workflowId))
            throw new InvalidOperationException($"SubWorkflow 节点 {node.Key} 缺少有效的 workflowId 配置");

        var version = node.Configs.TryGetValue("version", out var v) ? v?.ToString() : null;

        string canvasJson;
        if (!string.IsNullOrEmpty(version))
        {
            var wv = await _versionRepo.GetByVersionAsync(workflowId, version, context.CancellationToken)
                ?? throw new InvalidOperationException($"子流程 {workflowId} 版本 {version} 不存在");
            canvasJson = wv.CanvasJson;
        }
        else
        {
            var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, context.CancellationToken)
                ?? throw new InvalidOperationException($"子流程 {workflowId} 无草稿");
            canvasJson = draft.CanvasJson;
        }

        var canvas = JsonSerializer.Deserialize<CanvasSchema>(canvasJson)
            ?? throw new InvalidOperationException("子流程画布 JSON 无效");

        // 构建子流程输入
        var subInputs = new Dictionary<string, object?>();
        foreach (var (field, reference) in node.InputMappings)
        {
            subInputs[field] = context.GetVariable(reference);
        }

        var tenantId = _tenantProvider.GetTenantId();
        var subResult = await _dagExecutor.RunAsync(
            tenantId, workflowId, version, canvas, subInputs, context.CancellationToken);

        // 将子流程输出写入当前节点输出
        foreach (var (key, value) in subResult.Outputs)
        {
            context.SetOutput(node.Key, key, value);
        }

        return NodeExecutorResult.Default;
    }
}
