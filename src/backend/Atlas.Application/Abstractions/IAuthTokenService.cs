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

    Task<AuthTokenResult> CreateTokenForUserAsync(
        long userId,
        TenantId tenantId,
        AuthRequestContext context,
        CancellationToken cancellationToken);
}
