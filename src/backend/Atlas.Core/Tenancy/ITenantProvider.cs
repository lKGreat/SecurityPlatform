namespace Atlas.Core.Tenancy;

public interface ITenantProvider
{
    TenantId GetTenantId();
}