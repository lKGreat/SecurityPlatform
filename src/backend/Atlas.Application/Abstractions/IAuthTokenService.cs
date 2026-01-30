using Atlas.Application.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Abstractions;

public interface IAuthTokenService
{
    Task<AuthTokenResult> CreateTokenAsync(
        AuthTokenRequest request,
        TenantId tenantId,
        AuthRequestContext context,
        CancellationToken cancellationToken);

    Task<AuthTokenResult> RefreshTokenAsync(
        AuthRefreshRequest request,
        TenantId tenantId,
        AuthRequestContext context,
        CancellationToken cancellationToken);

    Task RevokeSessionAsync(
        long userId,
        TenantId tenantId,
        long sessionId,
        AuthRequestContext context,
        CancellationToken cancellationToken);
}
