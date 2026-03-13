using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using Atlas.Infrastructure.Services.WorkflowEngine;

namespace Atlas.Infrastructure.Services.AiPlatform;

public sealed class WorkflowV2QueryService : IWorkflowV2QueryService
{
    private readonly IWorkflowMetaRepository _metaRepo;
    private readonly IWorkflowDraftRepository _draftRepo;
    private readonly IWorkflowVersionRepository _versionRepo;
    private readonly IWorkflowExecutionRepository _executionRepo;
    private readonly IWorkflowNodeExecutionRepository _nodeExecutionRepo;
    private readonly NodeExecutorRegistry _registry;

    public WorkflowV2QueryService(
        IWorkflowMetaRepository metaRepo,
        IWorkflowDraftRepository draftRepo,
        IWorkflowVersionRepository versionRepo,
        IWorkflowExecutionRepository executionRepo,
        IWorkflowNodeExecutionRepository nodeExecutionRepo,
        NodeExecutorRegistry registry)
    {
        _metaRepo = metaRepo;
        _draftRepo = draftRepo;
        _versionRepo = versionRepo;
        _executionRepo = executionRepo;
        _nodeExecutionRepo = nodeExecutionRepo;
        _registry = registry;
    }

    public async Task<PagedResult<WorkflowV2ListItem>> ListAsync(
        TenantId tenantId, string? keyword, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        var (items, total) = await _metaRepo.GetPagedAsync(tenantId, keyword, pageIndex, pageSize, cancellationToken);
        var dtos = items.Select(MapListItem).ToList();
        return new PagedResult<WorkflowV2ListItem>(dtos, total, pageIndex, pageSize);
    }

    public async Task<WorkflowV2DetailDto?> GetAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        var meta = await _metaRepo.FindActiveByIdAsync(tenantId, id, cancellationToken);
        if (meta is null) return null;

        var draft = await _draftRepo.FindByWorkflowIdAsync(tenantId, meta.Id, cancellationToken);
        return MapDetail(meta, draft);
    }

    public async Task<IReadOnlyList<WorkflowV2VersionDto>> ListVersionsAsync(
        TenantId tenantId, long workflowId, CancellationToken cancellationToken)
    {
        var versions = await _versionRepo.ListByWorkflowIdAsync(tenantId, workflowId, cancellationToken);
        return versions.Select(MapVersion).ToList();
    }

    public async Task<WorkflowV2ExecutionDto?> GetExecutionProcessAsync(
        TenantId tenantId, long executionId, CancellationToken cancellationToken)
    {
        var execution = await _executionRepo.FindByIdAsync(tenantId, executionId, cancellationToken);
        if (execution is null) return null;

        var nodeExecutions = await _nodeExecutionRepo.ListByExecutionIdAsync(tenantId, executionId, cancellationToken);
        return MapExecution(execution, nodeExecutions);
    }

    public async Task<WorkflowV2NodeExecutionDto?> GetNodeExecutionDetailAsync(
        TenantId tenantId, long executionId, string nodeKey, CancellationToken cancellationToken)
    {
        var nodeExec = await _nodeExecutionRepo.FindByNodeKeyAsync(tenantId, executionId, nodeKey, cancellationToken);
        return nodeExec is null ? null : MapNodeExecution(nodeExec);
    }

    public Task<IReadOnlyList<WorkflowV2NodeTypeDto>> GetNodeTypesAsync(CancellationToken cancellationToken)
    {
        var types = _registry.GetAllTypes()
            .Select(m => new WorkflowV2NodeTypeDto(m.Key, m.Name, m.Category, m.Description))
            .ToList();
        return Task.FromResult<IReadOnlyList<WorkflowV2NodeTypeDto>>(types);
    }

    private static WorkflowV2ListItem MapListItem(WorkflowMeta meta)
        => new(meta.Id, meta.Name, meta.Description, meta.Mode, meta.Status,
            meta.LatestVersionNumber, meta.CreatorId, meta.CreatedAt, meta.UpdatedAt, meta.PublishedAt);

    private static WorkflowV2DetailDto MapDetail(WorkflowMeta meta, WorkflowDraft? draft)
        => new(meta.Id, meta.Name, meta.Description, meta.Mode, meta.Status,
            meta.LatestVersionNumber, meta.CreatorId,
            draft?.CanvasJson ?? "{}",
            draft?.CommitId,
            meta.CreatedAt, meta.UpdatedAt, meta.PublishedAt);

    private static WorkflowV2VersionDto MapVersion(WorkflowVersion v)
        => new(v.Id, v.WorkflowId, v.VersionNumber, v.ChangeLog, v.CanvasJson, v.PublishedAt, v.PublishedByUserId);

    private static WorkflowV2ExecutionDto MapExecution(WorkflowExecution exec, IReadOnlyList<WorkflowNodeExecution> nodes)
        => new(exec.Id, exec.WorkflowId, exec.VersionNumber, exec.Status,
            exec.InputsJson, exec.OutputsJson, exec.ErrorMessage,
            exec.StartedAt, exec.CompletedAt,
            nodes.Select(MapNodeExecution).ToList());

    private static WorkflowV2NodeExecutionDto MapNodeExecution(WorkflowNodeExecution n)
        => new(n.Id, n.ExecutionId, n.NodeKey, n.NodeType, n.Status,
            n.InputsJson, n.OutputsJson, n.ErrorMessage,
            n.StartedAt, n.CompletedAt, n.DurationMs);
}
