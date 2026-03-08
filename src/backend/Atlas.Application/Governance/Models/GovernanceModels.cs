namespace Atlas.Application.Governance.Models;

public sealed record PackageExportRequest(
    string ManifestId,
    string PackageType);

public sealed record PackageImportRequest(
    string FileName,
    string ContentBase64,
    string ConflictPolicy);

public sealed record PackageAnalyzeRequest(
    string FileName,
    string ContentBase64);

public sealed record PackageOperationResponse(
    string ArtifactId,
    string Status,
    string Message);

public sealed record LicenseOfflineRequest(
    string MachineFingerprint,
    string TenantId,
    string CustomerName);

public sealed record LicenseImportRequest(
    string LicenseContent);

public sealed record LicenseValidateResponse(
    bool IsValid,
    string Edition,
    string? ExpiresAt,
    string Message);

public sealed record ToolAuthorizationPolicyRequest(
    string ToolId,
    string ToolName,
    string PolicyType,
    int RateLimitQuota,
    string? ApprovalFlowId,
    string? ConditionJson,
    bool AuditEnabled);

public sealed record ToolAuthorizationPolicyResponse(
    string Id,
    string ToolId,
    string ToolName,
    string PolicyType,
    int RateLimitQuota,
    bool AuditEnabled);

public sealed record ToolAuthorizationSimulateRequest(
    string ToolId,
    string UserId,
    string? ContextJson);

public sealed record ToolAuthorizationSimulateResponse(
    string Decision,
    string PolicyId,
    int RemainingQuota);
