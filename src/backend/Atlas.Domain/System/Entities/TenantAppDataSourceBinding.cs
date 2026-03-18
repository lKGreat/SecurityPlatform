using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using SqlSugar;

namespace Atlas.Domain.System.Entities;

/// <summary>
/// 租户应用实例与数据源绑定关系（用于资源中心双层消费模型）
/// </summary>
public enum TenantAppDataSourceBindingType
{
    Primary = 0,
    Secondary = 1
}

public sealed class TenantAppDataSourceBinding : TenantEntity
{
    public TenantAppDataSourceBinding()
        : base(TenantId.Empty)
    {
        Note = string.Empty;
        BindingType = TenantAppDataSourceBindingType.Primary;
        IsActive = true;
    }

    public TenantAppDataSourceBinding(
        TenantId tenantId,
        long tenantAppInstanceId,
        long dataSourceId,
        TenantAppDataSourceBindingType bindingType,
        long boundByUserId,
        long id,
        DateTimeOffset now,
        string? note = null)
        : base(tenantId)
    {
        Id = id;
        TenantAppInstanceId = tenantAppInstanceId;
        DataSourceId = dataSourceId;
        BindingType = bindingType;
        IsActive = true;
        BoundByUserId = boundByUserId;
        BoundAt = now;
        UpdatedByUserId = boundByUserId;
        UpdatedAt = now;
        Note = note ?? string.Empty;
    }

    public long TenantAppInstanceId { get; private set; }

    public long DataSourceId { get; private set; }

    public TenantAppDataSourceBindingType BindingType { get; private set; }

    public bool IsActive { get; private set; }

    public long BoundByUserId { get; private set; }

    public DateTimeOffset BoundAt { get; private set; }

    public long UpdatedByUserId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public DateTimeOffset? UpdatedAt { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? Note { get; private set; }

    public void Rebind(
        long dataSourceId,
        TenantAppDataSourceBindingType bindingType,
        long updatedByUserId,
        DateTimeOffset now,
        string? note = null)
    {
        DataSourceId = dataSourceId;
        BindingType = bindingType;
        IsActive = true;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = now;
        Note = note ?? string.Empty;
    }

    public void Activate(long updatedByUserId, DateTimeOffset now, string? note = null)
    {
        IsActive = true;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = now;
        Note = note ?? string.Empty;
    }

    public void Deactivate(long updatedByUserId, DateTimeOffset now, string? note = null)
    {
        IsActive = false;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = now;
        Note = note ?? string.Empty;
    }
}
