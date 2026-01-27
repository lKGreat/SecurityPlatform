namespace Atlas.WebApi.Tenancy;

public sealed class TenancyOptions
{
    public string HeaderName { get; init; } = "X-Tenant-Id";
}
