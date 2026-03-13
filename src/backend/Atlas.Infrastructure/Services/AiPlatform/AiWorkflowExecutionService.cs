using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using Atlas.Infrastructure.Repositories;
using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.DSL.Interface;
using Atlas.WorkflowCore.Models;

namespace Atlas.Infrastructure.Services.AiPlatform;

public sealed class AiWorkflowExecutionService : IAiWorkflowExecutionService
{
    private readonly AiWorkflowDefinitionRepository _repository;
    private readonly IWorkflowHost _workflowHost;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IDefinitionLoader _definitionLoader;
    private readonly IPersistenceProvider _persistenceProvider;

    public AiWorkflowExecutionService(
        AiWorkflowDefinitionRepository repository,
        IWorkflowHost workflowHost,
        IWorkflowRegistry workflowRegistry,
        IDefinitionLoader definitionLoader,
        IPersistenceProvider persistenceProvider)
    {
        _repository = repository;
        _workflowHost = workflowHost;
        _workflowRegistry = workflowRegistry;
        _definitionLoader = definitionLoader;
        _persistenceProvider = persistenceProvider;
    }

    public async Task<AiWorkflowExecutionRunResult> RunAsync(
        TenantId tenantId,
        long workflowDefinitionId,
        AiWorkflowExecutionRunRequest request,
        CancellationToken cancellationToken)
    {
        var definitionEntity = await _repository.FindByIdAsync(tenantId, workflowDefinitionId, cancellationToken)
            ?? throw new BusinessException("工作流定义不存在。", ErrorCodes.NotFound);
        if (definitionEntity.Status != AiWorkflowStatus.Published)
        {
            throw new BusinessException("仅允许执行已发布的工作流。", ErrorCodes.ValidationError);
        }

        var definition = _definitionLoader.LoadDefinitionFromJson(definitionEntity.DefinitionJson);
        definition.Id = $"aiwf-{definitionEntity.Id}";
        definition.Version = Math.Max(1, definitionEntity.PublishVersion);
        _workflowRegistry.RegisterWorkflow(definition);

        var inputs = request.Inputs ?? new Dictionary<string, object?>();
        inputs["tenantId"] = tenantId.Value.ToString();
        inputs["workflowDefinitionId"] = workflowDefinitionId;

        var executionId = await _workflowHost.StartWorkflowAsync(
            definition.Id,
            definition.Version,
            inputs,
            reference: BuildTenantReference(tenantId, workflowDefinitionId),
            cancellationToken);

        return new AiWorkflowExecutionRunResult(executionId);
    }

    public async Task CancelAsync(TenantId tenantId, string executionId, CancellationToken cancellationToken)
    {
        var workflow = await GetOwnedWorkflowOrThrowAsync(tenantId, executionId, cancellationToken);
        if (workflow.Status == WorkflowStatus.Complete || workflow.Status == WorkflowStatus.Terminated)
        {
            return;
        }

        await _workflowHost.TerminateWorkflowAsync(executionId, cancellationToken);
    }

    public async Task<AiWorkflowExecutionProgressDto?> GetProgressAsync(
        TenantId tenantId,
        string executionId,
        CancellationToken cancellationToken)
    {
        var workflow = await GetOwnedWorkflowOrNullAsync(tenantId, executionId, cancellationToken);
        if (workflow is null) return null;

        return new AiWorkflowExecutionProgressDto(
            workflow.Id,
            workflow.WorkflowDefinitionId,
            workflow.Version,
            workflow.Status.ToString(),
            workflow.CreateTime,
            workflow.CompleteTime);
    }

    public async Task<IReadOnlyList<AiWorkflowNodeHistoryItem>> GetNodeHistoryAsync(
        TenantId tenantId,
        string executionId,
        CancellationToken cancellationToken)
    {
        var workflow = await GetOwnedWorkflowOrNullAsync(tenantId, executionId, cancellationToken);
        if (workflow is null) return [];

        return workflow.ExecutionPointers
            .OrderBy(x => x.StartTime ?? DateTime.MaxValue)
            .ThenBy(x => x.StepId)
            .Select(pointer => new AiWorkflowNodeHistoryItem(
                pointer.Id,
                pointer.StepId,
                pointer.StepName,
                pointer.Status.ToString(),
                pointer.StartTime,
                pointer.EndTime,
                pointer.Outcome))
            .ToList();
    }

    private async Task<Atlas.WorkflowCore.Models.WorkflowInstance?> GetOwnedWorkflowOrNullAsync(
        TenantId tenantId,
        string executionId,
        CancellationToken cancellationToken)
    {
        var workflow = await _persistenceProvider.GetWorkflowAsync(executionId, cancellationToken);
        if (workflow is null)
        {
            return null;
        }

        return IsWorkflowOwnedByTenant(workflow, tenantId) ? workflow : null;
    }

    private async Task<Atlas.WorkflowCore.Models.WorkflowInstance> GetOwnedWorkflowOrThrowAsync(
        TenantId tenantId,
        string executionId,
        CancellationToken cancellationToken)
    {
        var workflow = await GetOwnedWorkflowOrNullAsync(tenantId, executionId, cancellationToken);
        return workflow ?? throw new BusinessException("执行实例不存在。", ErrorCodes.NotFound);
    }

    private static bool IsWorkflowOwnedByTenant(Atlas.WorkflowCore.Models.WorkflowInstance workflow, TenantId tenantId)
    {
        var prefix = $"tenant:{tenantId.Value}:";
        return !string.IsNullOrWhiteSpace(workflow.Reference)
            && workflow.Reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTenantReference(TenantId tenantId, long workflowDefinitionId)
        => $"tenant:{tenantId.Value}:aiwf:{workflowDefinitionId}";
}
