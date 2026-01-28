using Atlas.Application.Approval.Models;
using FluentValidation;
using System.Text.Json;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 创建审批流定义的验证器
/// </summary>
public sealed class ApprovalFlowDefinitionCreateRequestValidator : AbstractValidator<ApprovalFlowDefinitionCreateRequest>
{
    public ApprovalFlowDefinitionCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("流程名称不能为空")
            .MaximumLength(100).WithMessage("流程名称长度不超过100个字符");

        RuleFor(x => x.DefinitionJson)
            .NotEmpty().WithMessage("流程定义JSON不能为空")
            .Custom((json, ctx) => ValidateDefinitionJson(json, ctx));
    }

    /// <summary>
    /// 验证定义 JSON 的结构安全性
    /// </summary>
    private static void ValidateDefinitionJson(string json, ValidationContext<ApprovalFlowDefinitionCreateRequest> ctx)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 基础结构检查
            if (!root.TryGetProperty("nodes", out var nodesElement) ||
                nodesElement.ValueKind != JsonValueKind.Array)
            {
                ctx.AddFailure("DefinitionJson", "定义JSON必须包含'nodes'数组");
                return;
            }

            if (!root.TryGetProperty("edges", out var edgesElement) ||
                edgesElement.ValueKind != JsonValueKind.Array)
            {
                ctx.AddFailure("DefinitionJson", "定义JSON必须包含'edges'数组");
                return;
            }

            var nodeIds = new HashSet<string>();
            var startNodeCount = 0;
            var endNodeCount = 0;

            // 验证节点
            foreach (var node in nodesElement.EnumerateArray())
            {
                if (!node.TryGetProperty("id", out var idProp))
                {
                    ctx.AddFailure("DefinitionJson", "每个节点必须有'id'属性");
                    return;
                }

                var nodeId = idProp.GetString();
                if (string.IsNullOrWhiteSpace(nodeId))
                {
                    ctx.AddFailure("DefinitionJson", "节点ID不能为空");
                    return;
                }

                if (nodeIds.Contains(nodeId))
                {
                    ctx.AddFailure("DefinitionJson", $"节点ID'{nodeId}'重复");
                    return;
                }

                nodeIds.Add(nodeId);

                // 检查节点类型
                if (!node.TryGetProperty("type", out var typeProp))
                {
                    ctx.AddFailure("DefinitionJson", $"节点'{nodeId}'必须有'type'属性");
                    return;
                }

                var nodeType = typeProp.GetString();
                if (nodeType == "start")
                    startNodeCount++;
                else if (nodeType == "end")
                    endNodeCount++;

                // 如果是条件节点，验证条件规则
                if (nodeType == "condition")
                {
                    if (node.TryGetProperty("conditionRule", out var ruleElement))
                    {
                        ValidateConditionRule(ruleElement, ctx);
                    }
                }
            }

            // 验证开始和结束节点数量
            if (startNodeCount != 1)
            {
                ctx.AddFailure("DefinitionJson", "流程必须有且仅有1个开始节点");
            }

            if (endNodeCount < 1)
            {
                ctx.AddFailure("DefinitionJson", "流程必须至少有1个结束节点");
            }

            // 验证边引用合法的节点
            foreach (var edge in edgesElement.EnumerateArray())
            {
                if (edge.TryGetProperty("source", out var sourceProp))
                {
                    var sourceId = sourceProp.GetString();
                    if (!string.IsNullOrEmpty(sourceId) && !nodeIds.Contains(sourceId))
                    {
                        ctx.AddFailure("DefinitionJson", $"边引用的源节点'{sourceId}'不存在");
                        return;
                    }
                }

                if (edge.TryGetProperty("target", out var targetProp))
                {
                    var targetId = targetProp.GetString();
                    if (!string.IsNullOrEmpty(targetId) && !nodeIds.Contains(targetId))
                    {
                        ctx.AddFailure("DefinitionJson", $"边引用的目标节点'{targetId}'不存在");
                        return;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            ctx.AddFailure("DefinitionJson", $"JSON格式无效: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证条件规则（仅允许白名单运算符，禁止脚本）
    /// </summary>
    private static void ValidateConditionRule(JsonElement ruleElement, ValidationContext<ApprovalFlowDefinitionCreateRequest> ctx)
    {
        const string allowedOperators = "equals,notEquals,greaterThan,lessThan,greaterThanOrEqual,lessThanOrEqual,in,contains,startsWith,endsWith";
        var allowedSet = allowedOperators.Split(',');

        if (ruleElement.TryGetProperty("operator", out var opProp))
        {
            var op = opProp.GetString();
            if (!string.IsNullOrEmpty(op) && !allowedSet.Contains(op))
            {
                ctx.AddFailure("DefinitionJson", $"条件规则运算符'{op}'不被允许，仅支持：{allowedOperators}");
            }
        }

        // 禁止包含脚本关键词
        var ruleJson = ruleElement.GetRawText();
        var forbiddenPatterns = new[] { "javascript:", "eval(", "function(", "script>" };
        foreach (var pattern in forbiddenPatterns)
        {
            if (ruleJson.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                ctx.AddFailure("DefinitionJson", $"条件规则包含不允许的内容：{pattern}");
                return;
            }
        }
    }
}
