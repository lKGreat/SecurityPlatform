using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 工具授权策略。
/// </summary>
public sealed class ToolAuthorizationPolicy : TenantEntity
{
    public ToolAuthorizationPolicy()
        : base(TenantId.Empty)
    {
        ToolId = string.Empty;
        ToolName = string.Empty;
        ConditionJson = "{}";
        PolicyType = ToolAuthorizationPolicyType.Allow;
        AuditEnabled = true;
    }

    public ToolAuthorizationPolicy(
        TenantId tenantId,
        long id,
        string toolId,
        string toolName,
        ToolAuthorizationPolicyType policyType,
        int? rateLimitQuota,
        long? approvalFlowId,
        string? conditionJson,
        bool auditEnabled)
        : base(tenantId)
    {
        SetId(id);
        ToolId = toolId;
        ToolName = toolName;
        PolicyType = policyType;
        RateLimitQuota = rateLimitQuota;
        ApprovalFlowId = approvalFlowId;
        ConditionJson = conditionJson ?? "{}";
        AuditEnabled = auditEnabled;
    }

    public string ToolId { get; private set; }

    public string ToolName { get; private set; }

    public ToolAuthorizationPolicyType PolicyType { get; private set; }

    public int? RateLimitQuota { get; private set; }

    public long? ApprovalFlowId { get; private set; }

    public string ConditionJson { get; private set; }

    public bool AuditEnabled { get; private set; }
}

public enum ToolAuthorizationPolicyType
{
    Allow = 1,
    Deny = 2,
    RequireApproval = 3
}

