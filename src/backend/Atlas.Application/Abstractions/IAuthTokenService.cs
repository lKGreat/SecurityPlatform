using Atlas.Application.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Abstractions;

public interface IAuthTokenService
{
    AuthTokenResult CreateToken(AuthTokenRequest request, TenantId tenantId);
}