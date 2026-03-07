using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 应用清单（产品化应用定义根）。
/// </summary>
public sealed class AppManifest : TenantEntity
{
    public AppManifest()
        : base(TenantId.Empty)
    {
        AppKey = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Icon = string.Empty;
        ConfigJson = "{}";
        Status = AppManifestStatus.Draft;
        Version = 1;
    }

    public AppManifest(
        TenantId tenantId,
        long id,
        string appKey,
        string name,
        string? description,
        string? category,
        string? icon,
        string? dataSourceId,
        long createdBy,
        DateTimeOffset now)
        : base(tenantId)
    {
        SetId(id);
        AppKey = appKey;
        Name = name;
        Description = description ?? string.Empty;
        Category = category ?? string.Empty;
        Icon = icon ?? string.Empty;
        DataSourceId = dataSourceId;
        ConfigJson = "{}";
        Status = AppManifestStatus.Draft;
        Version = 1;
        CreatedBy = createdBy;
        UpdatedBy = createdBy;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public string AppKey { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public string Category { get; private set; }

    public string Icon { get; private set; }

    public AppManifestStatus Status { get; private set; }

    public int Version { get; private set; }

    public string ConfigJson { get; private set; }

    public string? DataSourceId { get; private set; }

    public long CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public long UpdatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }
}

public enum AppManifestStatus
{
    Draft = 1,
    Published = 2,
    Disabled = 3,
    Archived = 4
}

