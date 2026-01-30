using Atlas.Application.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Application.Abstractions;

public interface IAuthProfileService
{
    Task<AuthProfileResult?> GetProfileAsync(
        long userId,
        TenantId tenantId,
        CancellationToken cancellationToken);
}
