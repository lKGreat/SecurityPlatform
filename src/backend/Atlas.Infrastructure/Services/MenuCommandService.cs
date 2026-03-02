using Atlas.Application.Identity.Abstractions;
using Atlas.Application.Identity.Models;
using Atlas.Application.Identity.Repositories;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Infrastructure.Services;

public sealed class MenuCommandService : IMenuCommandService
{
    private readonly IMenuRepository _menuRepository;

    public MenuCommandService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<long> CreateAsync(
        TenantId tenantId,
        MenuCreateRequest request,
        long id,
        CancellationToken cancellationToken)
    {
        var menu = new Menu(
            tenantId,
            request.Name,
            request.Path,
            id,
            request.ParentId,
            request.SortOrder,
            request.MenuType,
            request.Component,
            request.Icon,
            request.Perms,
            request.Query,
            request.IsFrame,
            request.IsCache,
            request.Visible,
            request.Status,
            request.PermissionCode,
            request.IsHidden);
        await _menuRepository.AddAsync(menu, cancellationToken);
        return menu.Id;
    }

    public async Task UpdateAsync(
        TenantId tenantId,
        long menuId,
        MenuUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var menu = await _menuRepository.FindByIdAsync(tenantId, menuId, cancellationToken);
        if (menu is null)
        {
            throw new BusinessException("Menu not found.", ErrorCodes.NotFound);
        }

        menu.Update(
            request.Name,
            request.Path,
            request.ParentId,
            request.SortOrder,
            request.MenuType,
            request.Component,
            request.Icon,
            request.Perms,
            request.Query,
            request.IsFrame,
            request.IsCache,
            request.Visible,
            request.Status,
            request.PermissionCode,
            request.IsHidden);
        await _menuRepository.UpdateAsync(menu, cancellationToken);
    }
}
