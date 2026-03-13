using System.Text.Json;
using Atlas.Application.Workflow.Abstractions.V2;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Entities;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

public sealed class WorkflowV2CommandService : IWorkflowV2CommandService
{
    private readonly IWorkflowMetaRepository _metaRepo;
    private readonly IWorkflowDraftRepository _draftRepo;
    private readonly IWorkflowVersionRepository _versionRepo;
    private readonly IIdGenerator _idGen;
    private readonly ITenantProvider _tenantProvider;

    public WorkflowV2CommandService(
        IWorkflowMetaRepository metaRepo,
        IWorkflowDraftRepository draftRepo,
        IWorkflowVersionRepository versionRepo,
        IIdGenerator idGen,
        ITenantProvider tenantProvider)
    {
        _metaRepo = metaRepo;
        _draftRepo = draftRepo;
        _versionRepo = versionRepo;
        _idGen = idGen;
        _tenantProvider = tenantProvider;
    }

    public async Task<long> CreateWorkflowAsync(WorkflowCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var id = _idGen.NextId();

        var meta = new WorkflowMeta(tenantId, id, request.Name);
        meta.UpdateMeta(request.Name, request.Description);
        meta.SetMode(request.Mode);
        await _metaRepo.AddAsync(meta, cancellationToken);

        var emptyCanvas = new CanvasSchema();
        var draftId = _idGen.NextId();
        var draft = new WorkflowDraft(tenantId, draftId, id, JsonSerializer.Serialize(emptyCanvas));
        await _draftRepo.AddAsync(draft, cancellationToken);

        return id;
    }

    public async Task SaveDraftAsync(long workflowId, WorkflowSaveRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var meta = await _metaRepo.GetByIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NOT_FOUND");

        var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, cancellationToken);
        if (draft is null)
        {
            var draftId = _idGen.NextId();
            draft = new WorkflowDraft(tenantId, draftId, workflowId, request.CanvasJson);
            await _draftRepo.AddAsync(draft, cancellationToken);
        }
        else
        {
            draft.Save(request.CanvasJson);
            await _draftRepo.UpdateAsync(draft, cancellationToken);
        }

        meta.Touch();
        await _metaRepo.UpdateAsync(meta, cancellationToken);
    }

    public async Task UpdateMetaAsync(long workflowId, WorkflowUpdateMetaRequest request, CancellationToken cancellationToken)
    {
        var meta = await _metaRepo.GetByIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NOT_FOUND");

        meta.UpdateMeta(request.Name, request.Description);
        await _metaRepo.UpdateAsync(meta, cancellationToken);
    }

    public async Task<WorkflowVersionItem> PublishAsync(long workflowId, WorkflowPublishRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var meta = await _metaRepo.GetByIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NOT_FOUND");

        var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ??????", "WORKFLOW_NO_DRAFT");

        var latestVersion = await _versionRepo.GetLatestAsync(workflowId, cancellationToken);
        var nextVersion = IncrementVersion(latestVersion?.Version);

        var versionId = _idGen.NextId();
        var version = new WorkflowVersion(tenantId, versionId, workflowId, nextVersion, draft.CommitId, draft.CanvasJson, request.ChangeLog);
        await _versionRepo.AddAsync(version, cancellationToken);

        meta.Publish(nextVersion);
        await _metaRepo.UpdateAsync(meta, cancellationToken);

        return new WorkflowVersionItem
        {
            Id = versionId,
            Version = nextVersion,
            CommitId = draft.CommitId,
            ChangeLog = request.ChangeLog,
            PublishedAt = version.PublishedAt
        };
    }

    public async Task DeleteWorkflowAsync(long workflowId, CancellationToken cancellationToken)
    {
        _ = await _metaRepo.GetByIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NOT_FOUND");

        await _metaRepo.DeleteAsync(workflowId, cancellationToken);
    }

    public async Task<long> CopyWorkflowAsync(long workflowId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var meta = await _metaRepo.GetByIdAsync(workflowId, cancellationToken)
            ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NOT_FOUND");

        var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, cancellationToken);

        var newId = _idGen.NextId();
        var newMeta = new WorkflowMeta(tenantId, newId, $"{meta.Name}_??");
        newMeta.UpdateMeta($"{meta.Name}_??", meta.Description);
        newMeta.SetMode(meta.Mode);
        await _metaRepo.AddAsync(newMeta, cancellationToken);

        var newDraftId = _idGen.NextId();
        var canvasJson = draft?.CanvasJson ?? JsonSerializer.Serialize(new CanvasSchema());
        var newDraft = new WorkflowDraft(tenantId, newDraftId, newId, canvasJson);
        await _draftRepo.AddAsync(newDraft, cancellationToken);

        return newId;
    }

    private static string IncrementVersion(string? currentVersion)
    {
        if (string.IsNullOrEmpty(currentVersion)) return "1.0.0";

        var parts = currentVersion.Split('.');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            int.TryParse(parts[2], out var patch))
        {
            return $"{major}.{minor}.{patch + 1}";
        }

        return "1.0.0";
    }
}
