using Atlas.Application.Identity.Models;
using Atlas.Application.Identity.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using Atlas.Application.Platform.Abstractions;

namespace Atlas.Infrastructure.Services.Platform;

public sealed class AppPermissionQueryService : IAppPermissionQueryService
{
    private readonly IPermissionRepository _repo;

    public AppPermissionQueryService(IPermissionRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<PermissionListItem>> QueryAsync(
        TenantId tenantId,
        long appId,
        PermissionQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var (items, total) = await _repo.QueryPageAsync(
            tenantId,
            pageIndex,
            pageSize,
            request.Keyword,
            request.Type,
            cancellationToken,
            appId: appId,
            platformOnly: false);
        var result = items.Select(x => new PermissionListItem(x.Id.ToString(), x.Name, x.Code, x.Type, x.Description)).ToArray();
        return new PagedResult<PermissionListItem>(result, total, pageIndex, pageSize);
    }

    public async Task<PermissionDetail?> GetByIdAsync(
        TenantId tenantId,
        long appId,
        long id,
        CancellationToken cancellationToken = default)
    {
        var x = await _repo.FindByIdAndAppIdAsync(tenantId, appId, id, cancellationToken);
        return x is null ? null : new PermissionDetail(x.Id.ToString(), x.Name, x.Code, x.Type, x.Description);
    }
}

public sealed class AppPermissionCommandService : IAppPermissionCommandService
{
    private readonly IPermissionRepository _repo;
    private readonly IIdGeneratorAccessor _idGen;

    public AppPermissionCommandService(IPermissionRepository repo, IIdGeneratorAccessor idGen)
    {
        _repo = repo;
        _idGen = idGen;
    }

    public async Task<long> CreateAsync(
        TenantId tenantId,
        long appId,
        PermissionCreateRequest request,
        long id,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repo.FindByCodeAsync(tenantId, request.Code.Trim(), appId, cancellationToken);
        if (existing is not null)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "AppPermissionCodeExists");
        }
        var entity = new Permission(tenantId, request.Name.Trim(), request.Code.Trim(), request.Type.Trim(), id, appId);
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            entity.Update(entity.Name, entity.Type, request.Description.Trim());
        }
        await _repo.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(
        TenantId tenantId,
        long appId,
        long id,
        PermissionUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repo.FindByIdAndAppIdAsync(tenantId, appId, id, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.NotFound, "AppScopedPermissionNotFound");
        entity.Update(request.Name.Trim(), request.Type.Trim(), request.Description?.Trim());
        await _repo.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(
        TenantId tenantId,
        long appId,
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repo.FindByIdAndAppIdAsync(tenantId, appId, id, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.NotFound, "AppScopedPermissionNotFound");
        await _repo.DeleteAsync(tenantId, entity.Id, cancellationToken);
    }
}
