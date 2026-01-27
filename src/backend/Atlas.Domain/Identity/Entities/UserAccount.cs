using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public sealed class UserAccount : TenantEntity
{
    public UserAccount()
        : base(TenantId.Empty)
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        Roles = string.Empty;
        IsActive = false;
        FailedLoginCount = 0;
        LastPasswordChangeAt = DateTimeOffset.UtcNow;
    }

    public UserAccount(TenantId tenantId, string username, string passwordHash, string roles)
        : base(tenantId)
    {
        Username = username;
        PasswordHash = passwordHash;
        Roles = roles;
        IsActive = true;
        FailedLoginCount = 0;
        LastPasswordChangeAt = DateTimeOffset.UtcNow;
    }

    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string Roles { get; private set; }
    public bool IsActive { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockoutEndAt { get; private set; }
    public bool IsManualLocked { get; private set; }
    public DateTimeOffset? ManualLockAt { get; private set; }
    public DateTimeOffset LastPasswordChangeAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public void UpdatePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        LastPasswordChangeAt = now;
        FailedLoginCount = 0;
        LockoutEndAt = null;
        IsManualLocked = false;
        ManualLockAt = null;
    }

    public void MarkLoginSuccess(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEndAt = null;
        IsManualLocked = false;
        ManualLockAt = null;
        LastLoginAt = now;
    }

    public void MarkLoginFailure(DateTimeOffset now, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount += 1;
        if (FailedLoginCount >= maxFailedAttempts)
        {
            LockoutEndAt = now.Add(lockoutDuration);
        }
    }

    public void ManualLock(DateTimeOffset now)
    {
        IsManualLocked = true;
        ManualLockAt = now;
    }

    public void Unlock()
    {
        FailedLoginCount = 0;
        LockoutEndAt = null;
        IsManualLocked = false;
        ManualLockAt = null;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
