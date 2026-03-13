using System.Text.Json;
using System.Threading.Channels;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Entities;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// DAG 工作流执行引擎。
/// 基于拓扑排序 + Task.WhenAll 并行分支执行，支持 If/Loop/SubWorkflow/中断。
/// </summary>
public sealed class DagExecutor
{
    private readonly NodeExecutorRegistry _registry;
    private readonly IWorkflowExecutionRepository _executionRepo;
    private readonly INodeExecutionRepository _nodeExecutionRepo;
    private readonly IIdGenerator _idGen;
    private readonly ILogger<DagExecutor> _logger;

    public DagExecutor(
        NodeExecutorRegistry registry,
        IWorkflowExecutionRepository executionRepo,
        INodeExecutionRepository nodeExecutionRepo,
        IIdGenerator idGen,
        ILogger<DagExecutor> logger)
    {
        _registry = registry;
        _executionRepo = executionRepo;
        _nodeExecutionRepo = nodeExecutionRepo;
        _idGen = idGen;
        _logger = logger;
    }

    /// <summary>
    /// 同步执行工作流，返回执行结果。
    /// </summary>
    public async Task<WorkflowRunResponse> RunAsync(
        TenantId tenantId,
        long workflowId,
        string? workflowVersion,
        CanvasSchema canvas,
        Dictionary<string, object?> inputs,
        CancellationToken cancellationToken)
    {
        var execId = _idGen.NextId();
        var execution = new WorkflowExecution(tenantId, execId, workflowId, workflowVersion, SerializeInputs(inputs));
        await _executionRepo.AddAsync(execution, cancellationToken);

        var context = new NodeExecutionContext(execId, canvas, inputs, null, cancellationToken);
        execution.Start();
        await _executionRepo.UpdateAsync(execution, cancellationToken);

        try
        {
            var outputs = await ExecuteGraphAsync(tenantId, context, canvas);
            execution.Complete(JsonSerializer.Serialize(outputs));
            await _executionRepo.UpdateAsync(execution, cancellationToken);

            return new WorkflowRunResponse
            {
                ExecutionId = execId,
                Status = ExecutionStatus.Success,
                Outputs = outputs,
                CostMs = execution.CostMs
            };
        }
        catch (OperationCanceledException)
        {
            execution.Cancel();
            await _executionRepo.UpdateAsync(execution, cancellationToken);
            return new WorkflowRunResponse
            {
                ExecutionId = execId,
                Status = ExecutionStatus.Cancelled,
                CostMs = execution.CostMs
            };
        }
        catch (WorkflowInterruptException ex)
        {
            execution.Interrupt(ex.InterruptType, ex.ContextJson);
            await _executionRepo.UpdateAsync(execution, cancellationToken);
            return new WorkflowRunResponse
            {
                ExecutionId = execId,
                Status = ExecutionStatus.Interrupted,
                CostMs = execution.CostMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流执行失败 ExecutionId={ExecutionId}", execId);
            execution.Fail(ex.Message);
            await _executionRepo.UpdateAsync(execution, cancellationToken);
            return new WorkflowRunResponse
            {
                ExecutionId = execId,
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                CostMs = execution.CostMs
            };
        }
    }

    /// <summary>
    /// 流式执行工作流，通过 Channel 推送 SSE 事件。
    /// </summary>
    public async Task StreamRunAsync(
        TenantId tenantId,
        long workflowId,
        string? workflowVersion,
        CanvasSchema canvas,
        Dictionary<string, object?> inputs,
        ChannelWriter<SseEvent> writer,
        CancellationToken cancellationToken)
    {
        var execId = _idGen.NextId();
        var execution = new WorkflowExecution(tenantId, execId, workflowId, workflowVersion, SerializeInputs(inputs));
        await _executionRepo.AddAsync(execution, cancellationToken);

        var channel = Channel.CreateUnbounded<SseEvent>();
        var context = new NodeExecutionContext(execId, canvas, inputs, channel, cancellationToken);
        execution.Start();
        await _executionRepo.UpdateAsync(execution, cancellationToken);

        // 把内部 channel 的事件转发到外部 writer
        var forwardTask = ForwardEventsAsync(channel.Reader, writer, cancellationToken);

        try
        {
            var startTime = DateTimeOffset.UtcNow;
            var outputs = await ExecuteGraphAsync(tenantId, context, canvas);
            execution.Complete(JsonSerializer.Serialize(outputs));
            await _executionRepo.UpdateAsync(execution, cancellationToken);

            await channel.Writer.WriteAsync(new SseEvent("workflow_done", new WorkflowDoneEvent
            {
                ExecutionId = execId,
                Status = "Success",
                TotalCostMs = execution.CostMs,
                Outputs = outputs
            }), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式工作流执行失败 ExecutionId={ExecutionId}", execId);
            execution.Fail(ex.Message);
            await _executionRepo.UpdateAsync(execution, cancellationToken);

            channel.Writer.TryWrite(new SseEvent("workflow_error", new WorkflowDoneEvent
            {
                ExecutionId = execId,
                Status = "Failed",
                TotalCostMs = execution.CostMs
            }));
        }
        finally
        {
            channel.Writer.Complete();
        }

        await forwardTask;
    }

    private async Task<Dictionary<string, object?>> ExecuteGraphAsync(
        TenantId tenantId,
        NodeExecutionContext context,
        CanvasSchema canvas)
    {
        // 找到入口节点
        var entryNode = canvas.Nodes.FirstOrDefault(n => n.Type == NodeType.Entry)
            ?? throw new InvalidOperationException("工作流缺少 Entry 节点");

        await ExecuteNodeChainAsync(tenantId, context, canvas, entryNode);

        // 收集 Exit 节点输出
        var exitOutputs = new Dictionary<string, object?>();
        var allData = context.GetAllData();
        var exitNode = canvas.Nodes.FirstOrDefault(n => n.Type == NodeType.Exit);
        if (exitNode is not null)
        {
            foreach (var (key, _) in exitNode.InputMappings)
            {
                exitOutputs[key] = allData.TryGetValue($"{exitNode.Key}.{key}", out var v) ? v : null;
            }
        }

        return exitOutputs;
    }

    private async Task ExecuteNodeChainAsync(
        TenantId tenantId,
        NodeExecutionContext context,
        CanvasSchema canvas,
        NodeSchema node)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (node.Type == NodeType.Exit)
        {
            await ExecuteSingleNodeAsync(tenantId, context, node);
            return;
        }

        var result = await ExecuteSingleNodeAsync(tenantId, context, node);

        if (result.NextPort is null) return;

        // 查找后继节点
        var outgoing = canvas.Connections
            .Where(c => c.FromNode == node.Key &&
                        (c.FromPort == null || c.FromPort == result.NextPort || result.NextPort == "default"))
            .ToList();

        if (outgoing.Count == 0) return;

        if (outgoing.Count == 1)
        {
            var next = canvas.Nodes.FirstOrDefault(n => n.Key == outgoing[0].ToNode);
            if (next is not null)
                await ExecuteNodeChainAsync(tenantId, context, canvas, next);
        }
        else
        {
            // 并行分支：Task.WhenAll
            var tasks = outgoing
                .Select(conn => canvas.Nodes.FirstOrDefault(n => n.Key == conn.ToNode))
                .Where(n => n is not null)
                .Select(n => ExecuteNodeChainAsync(tenantId, context, canvas, n!));
            await Task.WhenAll(tasks);
        }
    }

    private async Task<NodeExecutorResult> ExecuteSingleNodeAsync(
        TenantId tenantId,
        NodeExecutionContext context,
        NodeSchema node)
    {
        var nodeExecId = _idGen.NextId();
        var nodeRecord = new NodeExecution(tenantId, nodeExecId, context.ExecutionId, node.Key, node.Type, node.Title);

        // 解析输入
        var resolvedInputs = ResolveInputs(node, context);
        nodeRecord.Start(JsonSerializer.Serialize(resolvedInputs));
        await _nodeExecutionRepo.AddAsync(nodeRecord, CancellationToken.None);

        await context.EmitEventAsync(new SseEvent("node_start", new NodeStartEvent
        {
            ExecutionId = context.ExecutionId,
            NodeKey = node.Key,
            NodeType = node.Type.ToString(),
            NodeTitle = node.Title
        }));

        try
        {
            if (!_registry.TryGetExecutor(node.Type, out var executor) || executor is null)
            {
                // 未知节点类型：直接透传，当做 PassThrough
                context.SetOutputs(node.Key, resolvedInputs);
                nodeRecord.Complete(JsonSerializer.Serialize(resolvedInputs));
                await _nodeExecutionRepo.UpdateAsync(nodeRecord, CancellationToken.None);

                await context.EmitEventAsync(new SseEvent("node_complete", new NodeCompleteEvent
                {
                    ExecutionId = context.ExecutionId,
                    NodeKey = node.Key,
                    Status = "Success",
                    CostMs = nodeRecord.CostMs,
                    Output = resolvedInputs
                }));

                return NodeExecutorResult.Default;
            }

            var result = await executor.ExecuteAsync(node, context);
            var outputData = ExtractOutputs(node.Key, context);
            nodeRecord.Complete(JsonSerializer.Serialize(outputData));
            await _nodeExecutionRepo.UpdateAsync(nodeRecord, CancellationToken.None);

            await context.EmitEventAsync(new SseEvent("node_complete", new NodeCompleteEvent
            {
                ExecutionId = context.ExecutionId,
                NodeKey = node.Key,
                Status = "Success",
                CostMs = nodeRecord.CostMs,
                Output = outputData
            }));

            return result;
        }
        catch (WorkflowInterruptException)
        {
            nodeRecord.Complete("{}");
            await _nodeExecutionRepo.UpdateAsync(nodeRecord, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            nodeRecord.Fail(ex.Message);
            await _nodeExecutionRepo.UpdateAsync(nodeRecord, CancellationToken.None);

            await context.EmitEventAsync(new SseEvent("node_error", new NodeErrorEvent
            {
                ExecutionId = context.ExecutionId,
                NodeKey = node.Key,
                ErrorMessage = ex.Message,
                CostMs = nodeRecord.CostMs
            }));

            throw;
        }
    }

    private static Dictionary<string, object?> ResolveInputs(NodeSchema node, NodeExecutionContext context)
    {
        var resolved = new Dictionary<string, object?>();
        foreach (var (field, reference) in node.InputMappings)
        {
            resolved[field] = context.GetVariable(reference);
        }
        return resolved;
    }

    private static Dictionary<string, object?> ExtractOutputs(string nodeKey, NodeExecutionContext context)
    {
        var allData = context.GetAllData();
        var prefix = $"{nodeKey}.";
        return allData
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(kv => kv.Key[prefix.Length..], kv => kv.Value);
    }

    private static async Task ForwardEventsAsync(
        ChannelReader<SseEvent> reader,
        ChannelWriter<SseEvent> writer,
        CancellationToken cancellationToken)
    {
        await foreach (var evt in reader.ReadAllAsync(cancellationToken))
        {
            await writer.WriteAsync(evt, cancellationToken);
        }
    }

    private static string SerializeInputs(Dictionary<string, object?> inputs)
    {
        return JsonSerializer.Serialize(inputs);
    }
}

/// <summary>
/// 工作流中断异常，由 QuestionAnswer 等中断类节点抛出。
/// </summary>
public sealed class WorkflowInterruptException : Exception
{
    public WorkflowInterruptException(InterruptType interruptType, string contextJson)
        : base($"工作流中断: {interruptType}")
    {
        InterruptType = interruptType;
        ContextJson = contextJson;
    }

    public InterruptType InterruptType { get; }

    public string ContextJson { get; }
}
