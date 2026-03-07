using Atlas.Core.Abstractions;

namespace Atlas.Domain.Platform.Entities;

/// <summary>
/// 离线授权授予记录（全局实体，不隔离租户）。
/// </summary>
public sealed class LicenseGrant : EntityBase
{
    public LicenseGrant()
    {
        OfflineRequestToken = string.Empty;
        FeaturesJson = "{}";
        LimitsJson = "{}";
        AuditTrailJson = "[]";
        GrantMode = LicenseGrantMode.Online;
    }

    public LicenseGrant(
        long id,
        Guid licenseId,
        string offlineRequestToken,
        LicenseGrantMode grantMode,
        string featuresJson,
        string limitsJson,
        DateTimeOffset issuedAt,
        DateTimeOffset? expiresAt)
    {
        SetId(id);
        LicenseId = licenseId;
        OfflineRequestToken = offlineRequestToken;
        GrantMode = grantMode;
        FeaturesJson = featuresJson;
        LimitsJson = limitsJson;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        AuditTrailJson = "[]";
    }

    public Guid LicenseId { get; private set; }

    public string OfflineRequestToken { get; private set; }

    public LicenseGrantMode GrantMode { get; private set; }

    public int RenewalCount { get; private set; }

    public string FeaturesJson { get; private set; }

    public string LimitsJson { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public string AuditTrailJson { get; private set; }
}

public enum LicenseGrantMode
{
    Online = 1,
    Offline = 2
}

