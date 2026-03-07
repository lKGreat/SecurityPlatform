using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 应用发布记录（版本快照与回滚锚点）。
/// </summary>
public sealed class AppRelease : TenantEntity
{
    public AppRelease()
        : base(TenantId.Empty)
    {
        Version = string.Empty;
        ReleaseNote = string.Empty;
        SnapshotJson = "{}";
        Status = AppReleaseStatus.Pending;
    }

    public AppRelease(
        TenantId tenantId,
        long id,
        long manifestId,
        string version,
        string? releaseNote,
        string snapshotJson,
        long? rollbackPointId,
        long releasedBy,
        DateTimeOffset releasedAt)
        : base(tenantId)
    {
        SetId(id);
        ManifestId = manifestId;
        Version = version;
        ReleaseNote = releaseNote ?? string.Empty;
        SnapshotJson = snapshotJson;
        RollbackPointId = rollbackPointId;
        ReleasedBy = releasedBy;
        ReleasedAt = releasedAt;
        Status = AppReleaseStatus.Released;
    }

    public long ManifestId { get; private set; }

    public string Version { get; private set; }

    public string ReleaseNote { get; private set; }

    public string SnapshotJson { get; private set; }

    public long? RollbackPointId { get; private set; }

    public AppReleaseStatus Status { get; private set; }

    public long? ReleasedBy { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }
}

public enum AppReleaseStatus
{
    Pending = 1,
    Released = 2,
    RolledBack = 3
}

