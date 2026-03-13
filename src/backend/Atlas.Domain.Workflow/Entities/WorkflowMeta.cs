using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;

namespace Atlas.Domain.Workflow.Entities;

/// <summary>
/// 工作流元信息，对应 coze_workflows 表。
/// 包含工作流的基础属性，不含画布数据。
/// </summary>
public sealed class WorkflowMeta : TenantEntity
{
    public WorkflowMeta()
        : base(TenantId.Empty)
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public WorkflowMeta(TenantId tenantId, long id, string name)
        : base(tenantId)
    {
        SetId(id);
        Name = name;
        Description = string.Empty;
        Mode = WorkflowMode.Standard;
        Status = WorkflowLifecycleStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public WorkflowMode Mode { get; private set; }

    public WorkflowLifecycleStatus Status { get; private set; }

    /// <summary>最新发布版本号（草稿时为 null）</summary>
    public string? LatestVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateMeta(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetMode(WorkflowMode mode)
    {
        Mode = mode;
    }

    public void Publish(string version)
    {
        Status = WorkflowLifecycleStatus.Published;
        LatestVersion = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Disable()
    {
        Status = WorkflowLifecycleStatus.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
