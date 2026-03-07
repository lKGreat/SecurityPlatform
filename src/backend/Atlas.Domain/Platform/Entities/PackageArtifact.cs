using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 导入导出包元数据。
/// </summary>
public sealed class PackageArtifact : TenantEntity
{
    public PackageArtifact()
        : base(TenantId.Empty)
    {
        FilePath = string.Empty;
        FileHash = string.Empty;
        PackageType = PackageArtifactType.Structure;
        Status = PackageArtifactStatus.Pending;
    }

    public PackageArtifact(
        TenantId tenantId,
        long id,
        long manifestId,
        PackageArtifactType packageType,
        string filePath,
        string fileHash,
        long size,
        long exportedBy,
        DateTimeOffset exportedAt)
        : base(tenantId)
    {
        SetId(id);
        ManifestId = manifestId;
        PackageType = packageType;
        FilePath = filePath;
        FileHash = fileHash;
        Size = size;
        Status = PackageArtifactStatus.Exported;
        ExportedBy = exportedBy;
        ExportedAt = exportedAt;
    }

    public long ManifestId { get; private set; }

    public PackageArtifactType PackageType { get; private set; }

    public string FilePath { get; private set; }

    public string FileHash { get; private set; }

    public long Size { get; private set; }

    public PackageArtifactStatus Status { get; private set; }

    public long? ExportedBy { get; private set; }

    public DateTimeOffset? ExportedAt { get; private set; }

    public long? ImportedBy { get; private set; }

    public DateTimeOffset? ImportedAt { get; private set; }
}

public enum PackageArtifactType
{
    Structure = 1,
    Data = 2,
    Full = 3
}

public enum PackageArtifactStatus
{
    Pending = 1,
    Exported = 2,
    Imported = 3,
    Failed = 4
}

