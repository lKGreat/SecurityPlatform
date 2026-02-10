using System.Text.Json;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 将前端审批流设计器输出的 tree 结构（rootNode.childNode 链式）
/// 转换为后端引擎需要的 nodes[] + edges[] 图结构。
/// </summary>
public static class TreeToGraphConverter
{
    /// <summary>
    /// 判断 definitionJson 是否为 tree 格式（包含 nodes.rootNode 对象）
    /// </summary>
    public static bool IsTreeFormat(JsonElement root)
    {
        return root.TryGetProperty("nodes", out var nodesEl)
            && nodesEl.ValueKind == JsonValueKind.Object
            && nodesEl.TryGetProperty("rootNode", out _);
    }

    /// <summary>
    /// 从 tree 格式的 definitionJson 提取 nodes + edges
    /// </summary>
    public static (List<FlowNode> Nodes, List<FlowEdge> Edges) Convert(JsonElement root)
    {
        var nodes = new List<FlowNode>();
        var edges = new List<FlowEdge>();

        var rootNode = root.GetProperty("nodes").GetProperty("rootNode");
        FlattenNode(rootNode, nodes, edges);

        return (nodes, edges);
    }

    /// <summary>
    /// 递归展平 tree 节点为 FlowNode + FlowEdge
    /// </summary>
    private static void FlattenNode(
        JsonElement nodeEl,
        List<FlowNode> nodes,
        List<FlowEdge> edges)
    {
        var nodeId = GetString(nodeEl, "nodeId") ?? string.Empty;
        var nodeType = GetString(nodeEl, "nodeType") ?? string.Empty;
        var nodeName = GetString(nodeEl, "nodeName");

        if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(nodeType))
        {
            return;
        }

        // 构建 FlowNode
        var flowNode = new FlowNode
        {
            Id = nodeId,
            Type = MapNodeType(nodeType),
            Label = nodeName
        };

        // 解析 approve 节点的审批人配置
        if (nodeType == "approve")
        {
            ParseApproverConfig(nodeEl, flowNode);
        }

        nodes.Add(flowNode);

        // 解析 approve 节点的高级策略配置
        if (nodeType == "approve")
        {
            ParseApproveAdvancedConfig(nodeEl, flowNode);
        }

        // 解析 callProcess / timer / trigger 节点特有属性
        if (nodeType == "callProcess")
        {
            ParseCallProcessConfig(nodeEl, flowNode);
        }
        else if (nodeType == "timer")
        {
            ParseTimerConfig(nodeEl, flowNode);
        }
        else if (nodeType == "trigger")
        {
            ParseTriggerConfig(nodeEl, flowNode);
        }

        // 处理条件分支节点 (condition / dynamicCondition / parallelCondition / inclusive)
        if (nodeType is "condition" or "dynamicCondition" or "parallelCondition" or "inclusive")
        {
            HandleConditionBranches(nodeEl, nodeId, nodes, edges);
        }
        // 处理并行审批节点 (parallel)
        else if (nodeType == "parallel")
        {
            HandleParallelBranches(nodeEl, nodeId, nodes, edges);
        }

        // 处理 childNode -> 建立到下一个节点的边
        // 注意：条件/并行节点的 childNode 是汇聚后的下一个节点
        if (nodeEl.TryGetProperty("childNode", out var childEl) && childEl.ValueKind == JsonValueKind.Object)
        {
            var childId = GetString(childEl, "nodeId");
            if (!string.IsNullOrEmpty(childId))
            {
                // 条件/并行节点：childNode 是汇聚后的节点，边从各分支末端连到 childNode
                // 非条件/并行节点：直接连
                if (nodeType is not ("condition" or "dynamicCondition" or "parallelCondition" or "inclusive" or "parallel"))
                {
                    edges.Add(new FlowEdge { Source = nodeId, Target = childId });
                }

                FlattenNode(childEl, nodes, edges);
            }
        }
    }

    /// <summary>
    /// 处理条件分支：将条件节点映射为 exclusiveGateway，每个分支映射为分支边
    /// </summary>
    private static void HandleConditionBranches(
        JsonElement nodeEl,
        string gatewayNodeId,
        List<FlowNode> nodes,
        List<FlowEdge> edges)
    {
        if (!nodeEl.TryGetProperty("conditionNodes", out var branches) || branches.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        // 获取汇聚节点（conditionNode.childNode）
        string? mergeNodeId = null;
        if (nodeEl.TryGetProperty("childNode", out var mergeNodeEl) && mergeNodeEl.ValueKind == JsonValueKind.Object)
        {
            mergeNodeId = GetString(mergeNodeEl, "nodeId");
        }

        foreach (var branch in branches.EnumerateArray())
        {
            // 获取分支条件
            string? conditionRule = null;
            if (branch.TryGetProperty("conditionRule", out var ruleProp) && ruleProp.ValueKind != JsonValueKind.Null)
            {
                conditionRule = ruleProp.GetRawText();
            }

            // 分支的第一个子节点
            if (branch.TryGetProperty("childNode", out var branchChild) && branchChild.ValueKind == JsonValueKind.Object)
            {
                var branchChildId = GetString(branchChild, "nodeId");
                if (!string.IsNullOrEmpty(branchChildId))
                {
                    // 从网关到分支第一个节点的边（携带条件）
                    edges.Add(new FlowEdge
                    {
                        Source = gatewayNodeId,
                        Target = branchChildId,
                        ConditionRule = conditionRule
                    });

                    // 递归展平分支子树
                    FlattenNode(branchChild, nodes, edges);

                    // 从分支最后一个节点连到汇聚节点
                    if (!string.IsNullOrEmpty(mergeNodeId))
                    {
                        var lastNodeId = FindLastNodeId(branchChild);
                        if (!string.IsNullOrEmpty(lastNodeId) && lastNodeId != mergeNodeId)
                        {
                            edges.Add(new FlowEdge { Source = lastNodeId, Target = mergeNodeId });
                        }
                    }
                }
            }
            else
            {
                // 空分支：直接从网关连到汇聚节点
                if (!string.IsNullOrEmpty(mergeNodeId))
                {
                    edges.Add(new FlowEdge
                    {
                        Source = gatewayNodeId,
                        Target = mergeNodeId,
                        ConditionRule = conditionRule
                    });
                }
            }
        }
    }

    /// <summary>
    /// 处理并行分支：将并行节点映射为 parallelGateway split/join
    /// </summary>
    private static void HandleParallelBranches(
        JsonElement nodeEl,
        string splitGatewayId,
        List<FlowNode> nodes,
        List<FlowEdge> edges)
    {
        if (!nodeEl.TryGetProperty("parallelNodes", out var parallelBranches) || parallelBranches.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        // 获取汇聚节点（parallel.childNode）
        string? mergeNodeId = null;
        if (nodeEl.TryGetProperty("childNode", out var mergeNodeEl) && mergeNodeEl.ValueKind == JsonValueKind.Object)
        {
            mergeNodeId = GetString(mergeNodeEl, "nodeId");
        }

        // 如果有汇聚节点，创建一个 parallelGateway join 节点
        string? joinGatewayId = null;
        if (!string.IsNullOrEmpty(mergeNodeId))
        {
            joinGatewayId = splitGatewayId + "_join";
            nodes.Add(new FlowNode
            {
                Id = joinGatewayId,
                Type = "parallelGateway",
                Label = "并行汇聚"
            });

            // join 节点连到汇聚后的第一个节点
            edges.Add(new FlowEdge { Source = joinGatewayId, Target = mergeNodeId });
        }

        foreach (var branchNode in parallelBranches.EnumerateArray())
        {
            if (branchNode.ValueKind != JsonValueKind.Object) continue;

            var branchId = GetString(branchNode, "nodeId");
            if (string.IsNullOrEmpty(branchId)) continue;

            // split 网关到分支第一个节点
            edges.Add(new FlowEdge { Source = splitGatewayId, Target = branchId });

            // 递归展平分支
            FlattenNode(branchNode, nodes, edges);

            // 分支最后一个节点连到 join 网关
            if (!string.IsNullOrEmpty(joinGatewayId))
            {
                var lastNodeId = FindLastNodeId(branchNode);
                if (!string.IsNullOrEmpty(lastNodeId))
                {
                    edges.Add(new FlowEdge { Source = lastNodeId, Target = joinGatewayId });
                }
            }
        }
    }

    /// <summary>
    /// 从 approverConfig 中解析审批人信息
    /// </summary>
    private static void ParseApproverConfig(JsonElement nodeEl, FlowNode flowNode)
    {
        if (nodeEl.TryGetProperty("approverConfig", out var config) && config.ValueKind == JsonValueKind.Object)
        {
            // setType -> AssigneeType
            if (config.TryGetProperty("setType", out var setTypeProp) && setTypeProp.ValueKind == JsonValueKind.Number)
            {
                var setType = setTypeProp.GetInt32();
                if (Enum.IsDefined(typeof(AssigneeType), setType))
                {
                    flowNode.AssigneeType = (AssigneeType)setType;
                }
            }

            // signType -> ApprovalMode (1=All, 2=Any, 3=Sequential)
            if (config.TryGetProperty("signType", out var signTypeProp) && signTypeProp.ValueKind == JsonValueKind.Number)
            {
                flowNode.ApprovalMode = signTypeProp.GetInt32() switch
                {
                    2 => ApprovalMode.Any,
                    3 => ApprovalMode.Sequential,
                    _ => ApprovalMode.All
                };
            }

            // nodeApproveList -> AssigneeValue (逗号分隔 targetId)
            if (config.TryGetProperty("nodeApproveList", out var listProp) && listProp.ValueKind == JsonValueKind.Array)
            {
                var targetIds = new List<string>();
                foreach (var item in listProp.EnumerateArray())
                {
                    var targetId = GetString(item, "targetId");
                    if (!string.IsNullOrEmpty(targetId))
                    {
                        targetIds.Add(targetId);
                    }
                }
                if (targetIds.Count > 0)
                {
                    flowNode.AssigneeValue = string.Join(",", targetIds);
                }
            }

            // noHeaderAction -> MissingAssigneeStrategy
            if (config.TryGetProperty("noHeaderAction", out var noHeaderProp) && noHeaderProp.ValueKind == JsonValueKind.Number)
            {
                flowNode.MissingAssigneeStrategy = noHeaderProp.GetInt32() switch
                {
                    1 => MissingAssigneeStrategy.Skip,
                    2 => MissingAssigneeStrategy.TransferToAdmin,
                    _ => MissingAssigneeStrategy.NotAllowed
                };
            }
        }
        else
        {
            // 旧格式兼容：直接读取 assigneeType / assigneeValue / approvalMode
            if (nodeEl.TryGetProperty("assigneeType", out var atProp) && atProp.ValueKind == JsonValueKind.Number)
            {
                var at = atProp.GetInt32();
                if (Enum.IsDefined(typeof(AssigneeType), at))
                {
                    flowNode.AssigneeType = (AssigneeType)at;
                }
            }
            if (nodeEl.TryGetProperty("assigneeValue", out var avProp))
            {
                flowNode.AssigneeValue = avProp.GetString();
            }
            if (nodeEl.TryGetProperty("approvalMode", out var amProp))
            {
                var amStr = amProp.GetString();
                if (Enum.TryParse<ApprovalMode>(amStr, true, out var am))
                {
                    flowNode.ApprovalMode = am;
                }
            }
        }
    }

    /// <summary>
    /// 沿 childNode 链找到最后一个节点的 ID（不跟踪条件/并行分支的内部）
    /// </summary>
    private static string? FindLastNodeId(JsonElement nodeEl)
    {
        var current = nodeEl;
        while (true)
        {
            // 对于条件/并行节点，最后一个节点是其 childNode（汇聚后节点）
            // 如果没有 childNode，则本身就是最后一个节点
            if (current.TryGetProperty("childNode", out var child) && child.ValueKind == JsonValueKind.Object)
            {
                current = child;
            }
            else
            {
                return GetString(current, "nodeId");
            }
        }
    }

    /// <summary>
    /// 映射前端 nodeType 到后端引擎识别的 type
    /// </summary>
    private static string MapNodeType(string nodeType)
    {
        return nodeType switch
        {
            "start" => "start",
            "approve" => "approve",
            "copy" => "copy",
            "end" => "end",
            "condition" => "exclusiveGateway",
            "dynamicCondition" => "exclusiveGateway",
            "parallelCondition" => "exclusiveGateway",
            "parallel" => "parallelGateway",
            "inclusive" => "inclusiveGateway",
            "route" => "routeGateway",
            "callProcess" => "callProcess",
            "timer" => "timer",
            "trigger" => "trigger",
            _ => nodeType
        };
    }

    /// <summary>
    /// 解析 approve 节点的高级策略配置（驳回策略、重新审批策略、分组策略、票签、审批人=提交人等）
    /// </summary>
    private static void ParseApproveAdvancedConfig(JsonElement nodeEl, FlowNode flowNode)
    {
        // rejectStrategy
        if (nodeEl.TryGetProperty("rejectStrategy", out var rsProp) && rsProp.ValueKind == JsonValueKind.String)
        {
            var rsStr = rsProp.GetString();
            flowNode.RejectStrategy = rsStr switch
            {
                "toPrevious" => RejectStrategy.ToPrevious,
                "toInitiator" => RejectStrategy.ToInitiator,
                "toAnyNode" => RejectStrategy.ToAnyNode,
                "terminateApproval" => RejectStrategy.TerminateApproval,
                "toParentNode" => RejectStrategy.ToParentNode,
                _ => null
            };
        }

        // reApproveStrategy
        if (nodeEl.TryGetProperty("reApproveStrategy", out var rasProp) && rasProp.ValueKind == JsonValueKind.String)
        {
            var rasStr = rasProp.GetString();
            flowNode.ReApproveStrategy = rasStr switch
            {
                "continue" => ReApproveStrategy.Continue,
                "backToRejectNode" => ReApproveStrategy.BackToRejectNode,
                _ => null
            };
        }

        // groupStrategy
        if (nodeEl.TryGetProperty("groupStrategy", out var gsProp) && gsProp.ValueKind == JsonValueKind.String)
        {
            var gsStr = gsProp.GetString();
            flowNode.GroupStrategy = gsStr switch
            {
                "claim" => 0,
                "allParticipate" => 1,
                _ => null
            };
        }

        // voteWeight
        if (nodeEl.TryGetProperty("voteWeight", out var vwProp) && vwProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.Weight = vwProp.GetInt32();
        }

        // votePassRate
        if (nodeEl.TryGetProperty("votePassRate", out var vprProp) && vprProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.VotePassRate = vprProp.GetInt32();
        }

        // approveSelf
        if (nodeEl.TryGetProperty("approveSelf", out var asProp) && asProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.ApproveSelf = asProp.GetInt32();
        }

        // timeoutEnabled
        if (nodeEl.TryGetProperty("timeoutEnabled", out var teProp) && teProp.ValueKind == JsonValueKind.True)
        {
            flowNode.TimeoutEnabled = true;
        }

        // timeoutHours
        if (nodeEl.TryGetProperty("timeoutHours", out var thProp) && thProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.TimeoutHours = thProp.GetInt32();
        }

        // timeoutMinutes
        if (nodeEl.TryGetProperty("timeoutMinutes", out var tmProp) && tmProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.TimeoutMinutes = tmProp.GetInt32();
        }

        // timeoutAction
        if (nodeEl.TryGetProperty("timeoutAction", out var taProp) && taProp.ValueKind == JsonValueKind.String)
        {
            flowNode.TimeoutAction = taProp.GetString() switch
            {
                "autoApprove" => TimeoutAction.AutoApprove,
                "autoReject" => TimeoutAction.AutoReject,
                "autoSkip" => TimeoutAction.AutoSkip,
                _ => TimeoutAction.None
            };
        }

        // reminderIntervalHours
        if (nodeEl.TryGetProperty("reminderIntervalHours", out var rihProp) && rihProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.ReminderIntervalHours = rihProp.GetInt32();
        }

        // maxReminderCount
        if (nodeEl.TryGetProperty("maxReminderCount", out var mrcProp) && mrcProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.MaxReminderCount = mrcProp.GetInt32();
        }

        // callAi
        if (nodeEl.TryGetProperty("callAi", out var caProp) && caProp.ValueKind == JsonValueKind.True)
        {
            flowNode.CallAi = true;
        }

        // aiConfig
        if (nodeEl.TryGetProperty("aiConfig", out var acProp) && acProp.ValueKind == JsonValueKind.String)
        {
            flowNode.AiConfig = acProp.GetString();
        }
    }

    /// <summary>
    /// 解析 callProcess 节点属性
    /// </summary>
    private static void ParseCallProcessConfig(JsonElement nodeEl, FlowNode flowNode)
    {
        if (nodeEl.TryGetProperty("callProcessId", out var cpIdProp) && cpIdProp.ValueKind == JsonValueKind.Number)
        {
            flowNode.CallProcessId = cpIdProp.GetInt64();
        }

        if (nodeEl.TryGetProperty("callAsync", out var caProp))
        {
            flowNode.CallAsync = caProp.ValueKind == JsonValueKind.True;
        }
    }

    /// <summary>
    /// 解析 timer 节点属性
    /// </summary>
    private static void ParseTimerConfig(JsonElement nodeEl, FlowNode flowNode)
    {
        if (nodeEl.TryGetProperty("timerConfig", out var tcProp) && tcProp.ValueKind == JsonValueKind.String)
        {
            flowNode.TimerConfig = tcProp.GetString();
        }
    }

    /// <summary>
    /// 解析 trigger 节点属性
    /// </summary>
    private static void ParseTriggerConfig(JsonElement nodeEl, FlowNode flowNode)
    {
        if (nodeEl.TryGetProperty("triggerType", out var ttProp) && ttProp.ValueKind == JsonValueKind.String)
        {
            flowNode.TriggerType = ttProp.GetString();
        }
    }

    private static string? GetString(JsonElement el, string propertyName)
    {
        return el.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static int? GetInt(JsonElement el, string propertyName)
    {
        return el.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : null;
    }
}
