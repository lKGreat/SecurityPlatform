using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Domain.Approval.Entities;

/// <summary>
/// 审批流定义（包含节点、连线、配置的 JSON 模型）
/// </summary>
public sealed class ApprovalFlowDefinition : TenantEntity
{
    public ApprovalFlowDefinition()
        : base(TenantId.Empty)
    {
        Name = string.Empty;
        DefinitionJson = string.Empty;
    }

    public ApprovalFlowDefinition(TenantId tenantId, string name, string definitionJson, long id)
        : base(tenantId)
    {
        Id = id;
        Name = name;
        DefinitionJson = definitionJson;
        Version = 1;
        Status = ApprovalFlowStatus.Draft;
        PublishedAt = null;
        PublishedByUserId = null;
    }

    /// <summary>流程名称</summary>
    public string Name { get; private set; }

    /// <summary>流程定义 JSON（节点+连线+布局+节点配置）</summary>
    public string DefinitionJson { get; private set; }

    /// <summary>版本号</summary>
    public int Version { get; private set; }

    /// <summary>状态</summary>
    public ApprovalFlowStatus Status { get; private set; }

    /// <summary>发布时间</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>发布人 ID</summary>
    public long? PublishedByUserId { get; private set; }

    public void Update(string name, string definitionJson)
    {
        Name = name;
        DefinitionJson = definitionJson;
        Version += 1;
        if (Status != ApprovalFlowStatus.Draft)
        {
            Status = ApprovalFlowStatus.Draft;
        }
    }

    public void Publish(long publishedByUserId, DateTimeOffset now)
    {
        Status = ApprovalFlowStatus.Published;
        PublishedAt = now;
        PublishedByUserId = publishedByUserId;
    }

    public void Disable()
    {
        Status = ApprovalFlowStatus.Disabled;
    }

    public void Enable()
    {
        Status = ApprovalFlowStatus.Published;
    }
}
