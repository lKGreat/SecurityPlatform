using Atlas.Application.Visualization.Abstractions;
using Atlas.Application.Visualization.Models;
using Atlas.Core.Models;
using System.Text.Json;

namespace Atlas.Infrastructure.Services.Visualization;

/// <summary>
/// 可视化中心骨架实现：返回示例数据，便于前后端联调。
/// </summary>
public sealed class VisualizationQueryService : IVisualizationQueryService
{
    private static readonly string[] DefaultRisks = { "节点超时偏高", "部分流程版本未发布", "告警升级链路缺失回退" };

    public Task<VisualizationOverviewResponse> GetOverviewAsync(VisualizationFilterRequest filter, CancellationToken cancellationToken)
    {
        var overview = new VisualizationOverviewResponse(
            TotalProcesses: 12,
            RunningInstances: 128,
            BlockedNodes: 5,
            AlertsToday: 17,
            RiskHints: DefaultRisks);

        return Task.FromResult(overview);
    }

    public Task<PagedResult<VisualizationProcessSummary>> GetProcessesAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var items = Enumerable.Range(1, request.PageSize).Select(i => new VisualizationProcessSummary
        {
            Id = $"flow-{request.PageIndex}-{i}",
            Name = $"示例流程 {i}",
            Version = 1,
            Status = i % 2 == 0 ? "Published" : "Draft",
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-i)
        }).ToList();

        var result = new PagedResult<VisualizationProcessSummary>(
            items,
            42,
            request.PageIndex,
            request.PageSize);

        return Task.FromResult(result);
    }

    public Task<PagedResult<VisualizationInstanceSummary>> GetInstancesAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var items = Enumerable.Range(1, request.PageSize).Select(i => new VisualizationInstanceSummary
        {
            Id = $"inst-{request.PageIndex}-{i}",
            FlowName = $"示例流程 {i}",
            Status = i % 3 == 0 ? "Blocked" : "Running",
            CurrentNode = "审批节点",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-30 * i),
            DurationMinutes = 30 * i
        }).ToList();

        var result = new PagedResult<VisualizationInstanceSummary>(
            items,
            128,
            request.PageIndex,
            request.PageSize);

        return Task.FromResult(result);
    }

    public Task<VisualizationValidationResponse> ValidateAsync(ValidateVisualizationRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.DefinitionJson))
        {
            errors.Add("定义内容为空");
            return Task.FromResult(new VisualizationValidationResponse(false, errors));
        }

        try
        {
            var definition = JsonSerializer.Deserialize<CanvasDefinition>(request.DefinitionJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (definition == null || definition.Cells.Count == 0)
            {
                errors.Add("画布为空");
            }
            else
            {
                var nodes = definition.Cells.Where(c => !string.Equals(c.Shape, "edge", StringComparison.OrdinalIgnoreCase)).ToList();
                var edges = definition.Cells.Where(c => string.Equals(c.Shape, "edge", StringComparison.OrdinalIgnoreCase)).ToList();

                var startCount = nodes.Count(n => string.Equals(n.Data.Type, "start", StringComparison.OrdinalIgnoreCase));
                var endCount = nodes.Count(n => string.Equals(n.Data.Type, "end", StringComparison.OrdinalIgnoreCase));

                if (startCount != 1) errors.Add("必须且只能有一个开始节点");
                if (endCount < 1) errors.Add("至少需要一个结束节点");
                if (!edges.Any()) errors.Add("至少需要一条连线");

                // 边引用合法性
                var nodeIds = nodes.Select(n => n.Id).ToHashSet();
                foreach (var edge in edges)
                {
                    if (edge.Source?.Cell == null || edge.Target?.Cell == null)
                    {
                        errors.Add("存在缺失端点的连线");
                        break;
                    }
                    if (!nodeIds.Contains(edge.Source.Cell) || !nodeIds.Contains(edge.Target.Cell))
                    {
                        errors.Add("连线引用了不存在的节点");
                        break;
                    }
                }

                // 连通性校验（从 start 出发）
                var graph = nodeIds.ToDictionary(id => id, _ => new List<string>());
                foreach (var e in edges)
                {
                    if (e.Source?.Cell != null && e.Target?.Cell != null && graph.ContainsKey(e.Source.Cell))
                    {
                        graph[e.Source.Cell].Add(e.Target.Cell);
                    }
                }

                var startId = nodes.FirstOrDefault(n => string.Equals(n.Data.Type, "start", StringComparison.OrdinalIgnoreCase))?.Id;
                if (startId != null)
                {
                    var visited = new HashSet<string>();
                    void Dfs(string id)
                    {
                        if (!visited.Add(id)) return;
                        foreach (var nxt in graph[id]) Dfs(nxt);
                    }
                    Dfs(startId);
                    if (visited.Count < nodeIds.Count)
                    {
                        errors.Add("存在未连通节点，请检查连线");
                    }
                }

                // 条件节点必须有至少两个出度
                var conditionNodes = nodes.Where(n => string.Equals(n.Data.Type, "condition", StringComparison.OrdinalIgnoreCase));
                foreach (var cn in conditionNodes)
                {
                    var outDegree = graph.TryGetValue(cn.Id, out var outs) ? outs.Count : 0;
                    if (outDegree < 2)
                    {
                        errors.Add($"条件节点 {cn.Data.Name ?? cn.Id} 需要至少两个分支");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"解析错误: {ex.Message}");
        }

        var passed = errors.Count == 0;
        return Task.FromResult(new VisualizationValidationResponse(passed, errors));
    }

    public Task<VisualizationPublishResponse> PublishAsync(PublishVisualizationRequest request, CancellationToken cancellationToken)
    {
        var response = new VisualizationPublishResponse(request.ProcessId, request.Version, "Published");
        return Task.FromResult(response);
    }

    public Task<VisualizationInstanceDetail?> GetInstanceAsync(string id, CancellationToken cancellationToken)
    {
        var detail = new VisualizationInstanceDetail
        {
            Id = id,
            FlowName = "示例流程",
            Status = "Running",
            CurrentNode = "审批节点",
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Trace = new List<NodeTrace>
            {
                new() { NodeId = "start-1", Name = "开始", Status = "Completed", DurationMinutes = 1, StartedAt = DateTimeOffset.UtcNow.AddHours(-2), EndedAt = DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(1) },
                new() { NodeId = "approve-1", Name = "审批", Status = "Running", DurationMinutes = 30, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-30) }
            },
            RiskHints = new[] { "当前节点接近超时", "存在未连通路径需检查" }
        };

        return Task.FromResult<VisualizationInstanceDetail?>(detail);
    }
}
