namespace Atlas.Core.Tenancy;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString("D");
}