namespace Atlas.Application.Options;

public sealed class LockoutPolicyOptions
{
    public int MaxFailedAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public int AutoUnlockMinutes { get; init; } = 30;
    public bool AllowManualUnlock { get; init; } = true;
}
