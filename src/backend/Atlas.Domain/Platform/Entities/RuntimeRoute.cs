using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 运行态路由注册（appKey/pageKey -> 发布态 schema）。
/// </summary>
public sealed class RuntimeRoute : TenantEntity
{
    public RuntimeRoute()
        : base(TenantId.Empty)
    {
        AppKey = string.Empty;
        PageKey = string.Empty;
        EnvironmentCode = string.Empty;
        IsActive = true;
    }

    public RuntimeRoute(
        TenantId tenantId,
        long id,
        long manifestId,
        string appKey,
        string pageKey,
        int schemaVersion,
        string environmentCode,
        bool isActive)
        : base(tenantId)
    {
        SetId(id);
        ManifestId = manifestId;
        AppKey = appKey;
        PageKey = pageKey;
        SchemaVersion = schemaVersion;
        EnvironmentCode = environmentCode;
        IsActive = isActive;
    }

    public long ManifestId { get; private set; }

    public string AppKey { get; private set; }

    public string PageKey { get; private set; }

    public int SchemaVersion { get; private set; }

    public bool IsActive { get; private set; }

    public string EnvironmentCode { get; private set; }
}

