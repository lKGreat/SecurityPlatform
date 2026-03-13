using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Workflow.Entities;

/// <summary>
/// 工作流草稿，每个 WorkflowMeta 对应一条草稿记录。
/// 保存用户最近一次编辑的画布 JSON。
/// </summary>
public sealed class WorkflowDraft : TenantEntity
{
    public WorkflowDraft()
        : base(TenantId.Empty)
    {
        CanvasJson = string.Empty;
        CommitId = string.Empty;
    }

    public WorkflowDraft(TenantId tenantId, long id, long workflowId, string canvasJson)
        : base(tenantId)
    {
        SetId(id);
        WorkflowId = workflowId;
        CanvasJson = canvasJson;
        CommitId = Guid.NewGuid().ToString("N");
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public long WorkflowId { get; private set; }

    /// <summary>画布 JSON（序列化后的 CanvasSchema）</summary>
    public string CanvasJson { get; private set; }

    /// <summary>本次保存的唯一 commit 标识</summary>
    public string CommitId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Save(string canvasJson)
    {
        CanvasJson = canvasJson;
        CommitId = Guid.NewGuid().ToString("N");
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
