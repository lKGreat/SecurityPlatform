using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Atlas.Application.Workflow.Abstractions.V2;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

public sealed class WorkflowV2ExecutionService : IWorkflowV2ExecutionService
{
    private readonly DagExecutor _dagExecutor;
    private readonly NodeExecutorRegistry _nodeRegistry;
    private readonly IWorkflowMetaRepository _metaRepo;
    private readonly IWorkflowDraftRepository _draftRepo;
    private readonly IWorkflowVersionRepository _versionRepo;
    private readonly IWorkflowExecutionRepository _executionRepo;
    private readonly ITenantProvider _tenantProvider;
    private readonly IIdGenerator _idGen;

    public WorkflowV2ExecutionService(
        DagExecutor dagExecutor,
        NodeExecutorRegistry nodeRegistry,
        IWorkflowMetaRepository metaRepo,
        IWorkflowDraftRepository draftRepo,
        IWorkflowVersionRepository versionRepo,
        IWorkflowExecutionRepository executionRepo,
        ITenantProvider tenantProvider,
        IIdGenerator idGen)
    {
        _dagExecutor = dagExecutor;
        _nodeRegistry = nodeRegistry;
        _metaRepo = metaRepo;
        _draftRepo = draftRepo;
        _versionRepo = versionRepo;
        _executionRepo = executionRepo;
        _tenantProvider = tenantProvider;
        _idGen = idGen;
    }

    public async Task<WorkflowRunResponse> SyncRunAsync(long workflowId, WorkflowRunRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var canvas = await ResolveCanvasAsync(workflowId, request.Version, cancellationToken);
        return await _dagExecutor.RunAsync(tenantId, workflowId, request.Version, canvas, request.Inputs, cancellationToken);
    }

    public async Task<long> AsyncRunAsync(long workflowId, WorkflowRunRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var canvas = await ResolveCanvasAsync(workflowId, request.Version, cancellationToken);

        var inputs = request.Inputs;
        var version = request.Version;

        var execId = _idGen.NextId();
        var execution = new Domain.Workflow.Entities.WorkflowExecution(tenantId, execId, workflowId, version, JsonSerializer.Serialize(inputs));
        await _executionRepo.AddAsync(execution, cancellationToken);

        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            try
            {
                await _dagExecutor.RunAsync(tenantId, workflowId, version, canvas, inputs, cts.Token);
            }
            catch (Exception)
            {
                // ????????? DagExecutor ?????
            }
        });

        return execId;
    }

    public async Task CancelAsync(long executionId, CancellationToken cancellationToken)
    {
        var execution = await _executionRepo.GetByIdAsync(executionId, cancellationToken)
            ?? throw new BusinessException($"???? {executionId} ???", "EXECUTION_NOT_FOUND");

        if (execution.Status is ExecutionStatus.Success or ExecutionStatus.Failed or ExecutionStatus.Cancelled)
            throw new BusinessException("??????????", "EXECUTION_ALREADY_COMPLETED");

        execution.Cancel();
        await _executionRepo.UpdateAsync(execution, cancellationToken);
    }

    public async Task ResumeAsync(long executionId, WorkflowResumeRequest request, CancellationToken cancellationToken)
    {
        var execution = await _executionRepo.GetByIdAsync(executionId, cancellationToken)
            ?? throw new BusinessException($"???? {executionId} ???", "EXECUTION_NOT_FOUND");

        if (execution.Status != ExecutionStatus.Interrupted)
            throw new BusinessException("???????????", "EXECUTION_NOT_INTERRUPTED");

        var tenantId = _tenantProvider.GetTenantId();
        var canvas = await ResolveCanvasAsync(execution.WorkflowId, execution.WorkflowVersion, cancellationToken);

        var inputs = request.Data;
        if (execution.InterruptContextJson is not null)
        {
            var ctx = JsonSerializer.Deserialize<Dictionary<string, string>>(execution.InterruptContextJson);
            if (ctx is not null && ctx.TryGetValue("nodeKey", out var nodeKey))
            {
                inputs["__resume_nodeKey"] = nodeKey;
            }
        }

        execution.Resume();
        await _executionRepo.UpdateAsync(execution, cancellationToken);

        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            await _dagExecutor.RunAsync(tenantId, execution.WorkflowId, execution.WorkflowVersion, canvas, inputs, cts.Token);
        });
    }

    public async Task<NodeDebugResponse> DebugNodeAsync(long workflowId, NodeDebugRequest request, CancellationToken cancellationToken)
    {
        var canvas = await ResolveCanvasAsync(workflowId, null, cancellationToken);
        var node = canvas.Nodes.FirstOrDefault(n => n.Key == request.NodeKey)
            ?? throw new BusinessException($"???????? {request.NodeKey}", "NODE_NOT_FOUND");

        var context = new NodeExecutionContext(_idGen.NextId(), canvas, request.Inputs, null, cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        try
        {
            if (!_nodeRegistry.TryGetExecutor(node.Type, out var executor) || executor is null)
            {
                return new NodeDebugResponse
                {
                    NodeKey = node.Key,
                    Status = ExecutionStatus.Success,
                    Inputs = request.Inputs,
                    Outputs = request.Inputs,
                    CostMs = 0
                };
            }

            await executor.ExecuteAsync(node, context);
            var prefix = $"{node.Key}.";
            var outputs = context.GetAllData()
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                .ToDictionary(kv => kv.Key[prefix.Length..], kv => kv.Value);

            return new NodeDebugResponse
            {
                NodeKey = node.Key,
                Status = ExecutionStatus.Success,
                Inputs = request.Inputs,
                Outputs = outputs,
                CostMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            return new NodeDebugResponse
            {
                NodeKey = node.Key,
                Status = ExecutionStatus.Failed,
                Inputs = request.Inputs,
                Outputs = new Dictionary<string, object?>(),
                ErrorMessage = ex.Message,
                CostMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async IAsyncEnumerable<SseEvent> StreamRunAsync(
        long workflowId,
        WorkflowRunRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var canvas = await ResolveCanvasAsync(workflowId, request.Version, cancellationToken);

        var channel = Channel.CreateUnbounded<SseEvent>();

        _ = Task.Run(async () =>
        {
            try
            {
                await _dagExecutor.StreamRunAsync(tenantId, workflowId, request.Version, canvas, request.Inputs, channel.Writer, cancellationToken);
            }
            catch (Exception)
            {
                channel.Writer.TryComplete();
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    private async Task<CanvasSchema> ResolveCanvasAsync(long workflowId, string? version, CancellationToken cancellationToken)
    {
        string canvasJson;

        if (!string.IsNullOrEmpty(version))
        {
            var wv = await _versionRepo.GetByVersionAsync(workflowId, version, cancellationToken)
                ?? throw new BusinessException($"??? {workflowId} ?? {version} ???", "WORKFLOW_VERSION_NOT_FOUND");
            canvasJson = wv.CanvasJson;
        }
        else
        {
            var draft = await _draftRepo.GetByWorkflowIdAsync(workflowId, cancellationToken)
                ?? throw new BusinessException($"??? {workflowId} ???", "WORKFLOW_NO_DRAFT");
            canvasJson = draft.CanvasJson;
        }

        return JsonSerializer.Deserialize<CanvasSchema>(canvasJson)
            ?? throw new BusinessException("????? JSON ??", "WORKFLOW_INVALID_CANVAS");
    }
}
