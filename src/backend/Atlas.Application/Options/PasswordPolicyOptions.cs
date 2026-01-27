namespace Atlas.Application.Options;

public sealed class PasswordPolicyOptions
{
    public int MinLength { get; init; } = 8;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireNonAlphanumeric { get; init; } = true;
    public int ExpirationDays { get; init; } = 90;
}
