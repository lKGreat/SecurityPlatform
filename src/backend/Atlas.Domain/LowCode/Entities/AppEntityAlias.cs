using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.LowCode.Entities;

/// <summary>
/// 应用级实体别名（用于运行态术语定制）
/// </summary>
public sealed class AppEntityAlias : TenantEntity
{
    public AppEntityAlias()
        : base(TenantId.Empty)
    {
        EntityType = string.Empty;
        SingularAlias = string.Empty;
        PluralAlias = string.Empty;
    }

    public AppEntityAlias(
        TenantId tenantId,
        long appId,
        string entityType,
        string singularAlias,
        string pluralAlias,
        long updatedBy,
        long id,
        DateTimeOffset now)
        : base(tenantId)
    {
        Id = id;
        AppId = appId;
        EntityType = entityType;
        SingularAlias = singularAlias;
        PluralAlias = pluralAlias;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    public long AppId { get; private set; }

    public string EntityType { get; private set; }

    public string SingularAlias { get; private set; }

    public string PluralAlias { get; private set; }

    public long UpdatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateAlias(string singularAlias, string pluralAlias, long updatedBy, DateTimeOffset now)
    {
        SingularAlias = singularAlias;
        PluralAlias = pluralAlias;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }
}
