using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Atlas.Application.AiPlatform.Models;
using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using Atlas.Domain.AiPlatform.Enums;
using Atlas.Domain.AiPlatform.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// V2 DAG 执行引擎——按拓扑顺序执行工作流画布中的节点。
/// </summary>
public sealed class DagExecutor
{
    private readonly NodeExecutorRegistry _registry;
    private readonly IWorkflowNodeExecutionRepository _nodeExecutionRepo;
    private readonly IWorkflowExecutionRepository _executionRepo;
    private readonly IIdGeneratorAccessor _idGenerator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DagExecutor> _logger;

    public DagExecutor(
        NodeExecutorRegistry registry,
        IWorkflowNodeExecutionRepository nodeExecutionRepo,
        IWorkflowExecutionRepository executionRepo,
        IIdGeneratorAccessor idGenerator,
        IServiceProvider serviceProvider,
        ILogger<DagExecutor> logger)
    {
        _registry = registry;
        _nodeExecutionRepo = nodeExecutionRepo;
        _executionRepo = executionRepo;
        _idGenerator = idGenerator;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 同步执行工作流 DAG 图。
    /// </summary>
    public async Task RunAsync(
        TenantId tenantId,
        WorkflowExecution execution,
        CanvasSchema canvas,
        Dictionary<string, string> inputs,
        Channel<SseEvent>? eventChannel,
        CancellationToken cancellationToken)
    {
        execution.Start();
        await _executionRepo.UpdateAsync(execution, cancellationToken);

        var variables = new Dictionary<string, string>(inputs, StringComparer.OrdinalIgnoreCase);

        try
        {
            // 构建邻接表
            var nodeMap = canvas.Nodes.ToDictionary(n => n.Key, n => n, StringComparer.OrdinalIgnoreCase);
            var adjacency = BuildAdjacency(canvas);
            var executionOrder = TopologicalSort(canvas.Nodes, adjacency);

            foreach (var nodeKey in executionOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!nodeMap.TryGetValue(nodeKey, out var node))
                {
                    continue;
                }

                var executor = _registry.GetExecutor(node.Type);
                if (executor is null)
                {
                    _logger.LogWarning("未找到节点类型 {NodeType} 的执行器，跳过节点 {NodeKey}", node.Type, nodeKey);
                    continue;
                }

                // 创建节点执行记录
                var nodeExec = new WorkflowNodeExecution(tenantId, execution.Id, nodeKey, node.Type, _idGenerator.NextId());
                nodeExec.Start(JsonSerializer.Serialize(variables));
                await _nodeExecutionRepo.AddAsync(nodeExec, cancellationToken);

                if (eventChannel is not null)
                {
                    await eventChannel.Writer.WriteAsync(
                        new SseEvent("node_start", JsonSerializer.Serialize(new { nodeKey, nodeType = node.Type.ToString() })),
                        cancellationToken);
                }

                var sw = Stopwatch.StartNew();
                var context = new NodeExecutionContext(node, variables, _serviceProvider, execution.Id, eventChannel);

                try
                {
                    var result = await executor.ExecuteAsync(context, cancellationToken);
                    sw.Stop();

                    if (result.Success)
                    {
                        // 将输出合并到变量
                        foreach (var kvp in result.Outputs)
                        {
                            variables[kvp.Key] = kvp.Value;
                        }

                        nodeExec.Complete(JsonSerializer.Serialize(result.Outputs), sw.ElapsedMilliseconds);
                        await _nodeExecutionRepo.UpdateAsync(nodeExec, cancellationToken);
                    }
                    else
                    {
                        nodeExec.Fail(result.ErrorMessage ?? "节点执行失败");
                        await _nodeExecutionRepo.UpdateAsync(nodeExec, cancellationToken);

                        if (result.InterruptType != InterruptType.None)
                        {
                            execution.Interrupt(result.InterruptType, nodeKey);
                            await _executionRepo.UpdateAsync(execution, cancellationToken);
                            return;
                        }

                        // 非中断型失败直接终止
                        execution.Fail(result.ErrorMessage ?? "节点执行失败");
                        await _executionRepo.UpdateAsync(execution, cancellationToken);
                        return;
                    }

                    if (eventChannel is not null)
                    {
                        await eventChannel.Writer.WriteAsync(
                            new SseEvent("node_complete", JsonSerializer.Serialize(new { nodeKey, durationMs = sw.ElapsedMilliseconds })),
                            cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    nodeExec.Fail("执行已取消");
                    await _nodeExecutionRepo.UpdateAsync(nodeExec, cancellationToken);
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "节点 {NodeKey} 执行异常", nodeKey);
                    nodeExec.Fail(ex.Message);
                    await _nodeExecutionRepo.UpdateAsync(nodeExec, cancellationToken);

                    execution.Fail($"节点 {nodeKey} 执行异常: {ex.Message}");
                    await _executionRepo.UpdateAsync(execution, cancellationToken);
                    return;
                }
            }

            // 全部执行完毕
            cancellationToken.ThrowIfCancellationRequested();
            var latestExecution = await _executionRepo.FindByIdAsync(tenantId, execution.Id, CancellationToken.None);
            if (latestExecution?.Status == ExecutionStatus.Cancelled)
            {
                _logger.LogInformation("执行已被取消，跳过完成态回写: ExecutionId={ExecutionId}", execution.Id);
                return;
            }

            execution.Complete(JsonSerializer.Serialize(variables));
            await _executionRepo.UpdateAsync(execution, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            execution.Cancel();
            await _executionRepo.UpdateAsync(execution, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流执行异常: ExecutionId={ExecutionId}", execution.Id);
            execution.Fail(ex.Message);
            await _executionRepo.UpdateAsync(execution, CancellationToken.None);
        }
        finally
        {
            eventChannel?.Writer.TryComplete();
        }
    }

    /// <summary>
    /// 从 CanvasJson 反序列化 CanvasSchema。
    /// </summary>
    public static CanvasSchema? ParseCanvas(string canvasJson)
    {
        if (string.IsNullOrWhiteSpace(canvasJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CanvasSchema>(canvasJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, List<string>> BuildAdjacency(CanvasSchema canvas)
    {
        var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in canvas.Nodes)
        {
            adjacency.TryAdd(node.Key, new List<string>());
        }

        foreach (var conn in canvas.Connections)
        {
            if (adjacency.TryGetValue(conn.SourceNodeKey, out var targets))
            {
                targets.Add(conn.TargetNodeKey);
            }
        }

        return adjacency;
    }

    private static List<string> TopologicalSort(
        IReadOnlyList<NodeSchema> nodes,
        Dictionary<string, List<string>> adjacency)
    {
        var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            indegree.TryAdd(node.Key, 0);
        }

        foreach (var targets in adjacency.Values)
        {
            foreach (var target in targets)
            {
                if (indegree.ContainsKey(target))
                {
                    indegree[target]++;
                }
            }
        }

        var queue = new Queue<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key));
        var ordered = new List<string>(nodes.Count);

        while (queue.Count > 0)
        {
            var key = queue.Dequeue();
            ordered.Add(key);

            if (!adjacency.TryGetValue(key, out var targets))
            {
                continue;
            }

            foreach (var target in targets)
            {
                if (!indegree.ContainsKey(target))
                {
                    continue;
                }

                indegree[target]--;
                if (indegree[target] == 0)
                {
                    queue.Enqueue(target);
                }
            }
        }

        if (ordered.Count != nodes.Count)
        {
            var cycleNodes = indegree
                .Where(x => x.Value > 0)
                .Select(x => x.Key)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            throw new InvalidOperationException(
                $"检测到工作流环路，涉及节点: {string.Join(", ", cycleNodes)}。请移除循环依赖后重试。");
        }

        return ordered;
    }
}
