using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Workflow.Entities;

/// <summary>
/// 工作流已发布版本，保存不可变的版本快照。
/// SemVer 格式：主版本.次版本.修订号（如 "1.0.0"）。
/// </summary>
public sealed class WorkflowVersion : TenantEntity
{
    public WorkflowVersion()
        : base(TenantId.Empty)
    {
        Version = string.Empty;
        CommitId = string.Empty;
        CanvasJson = string.Empty;
        ChangeLog = string.Empty;
    }

    public WorkflowVersion(TenantId tenantId, long id, long workflowId, string version, string commitId, string canvasJson, string changeLog)
        : base(tenantId)
    {
        SetId(id);
        WorkflowId = workflowId;
        Version = version;
        CommitId = commitId;
        CanvasJson = canvasJson;
        ChangeLog = changeLog;
        PublishedAt = DateTimeOffset.UtcNow;
    }

    public long WorkflowId { get; private set; }

    /// <summary>SemVer 版本号，如 "1.0.0"</summary>
    public string Version { get; private set; }

    /// <summary>对应 WorkflowDraft.CommitId，发布时快照</summary>
    public string CommitId { get; private set; }

    /// <summary>该版本的画布 JSON 快照（发布时冻结）</summary>
    public string CanvasJson { get; private set; }

    public string ChangeLog { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }
}
