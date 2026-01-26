namespace Atlas.Core.Tenancy;

public interface ITenantScoped
{
    TenantId TenantId { get; }
}