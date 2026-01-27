using Atlas.Application.Identity.Abstractions;
using Atlas.Application.Identity.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

[ApiController]
[Route("menus")]
public sealed class MenusController : ControllerBase
{
    private readonly IMenuQueryService _menuQueryService;
    private readonly IMenuCommandService _menuCommandService;
    private readonly ITenantProvider _tenantProvider;
    private readonly Atlas.Core.Abstractions.IIdGenerator _idGenerator;
    private readonly IValidator<MenuCreateRequest> _createValidator;
    private readonly IValidator<MenuUpdateRequest> _updateValidator;

    public MenusController(
        IMenuQueryService menuQueryService,
        IMenuCommandService menuCommandService,
        ITenantProvider tenantProvider,
        Atlas.Core.Abstractions.IIdGenerator idGenerator,
        IValidator<MenuCreateRequest> createValidator,
        IValidator<MenuUpdateRequest> updateValidator)
    {
        _menuQueryService = menuQueryService;
        _menuCommandService = menuCommandService;
        _tenantProvider = tenantProvider;
        _idGenerator = idGenerator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<MenuListItem>>>> Get(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var result = await _menuQueryService.QueryMenusAsync(request, tenantId, cancellationToken);
        return Ok(ApiResponse<PagedResult<MenuListItem>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MenuListItem>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var result = await _menuQueryService.QueryAllAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MenuListItem>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] MenuCreateRequest request,
        CancellationToken cancellationToken)
    {
        _createValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        var id = _idGenerator.NextId();
        var createdId = await _menuCommandService.CreateAsync(tenantId, request, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Id = createdId.ToString() }, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        long id,
        [FromBody] MenuUpdateRequest request,
        CancellationToken cancellationToken)
    {
        _updateValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        await _menuCommandService.UpdateAsync(tenantId, id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Id = id.ToString() }, HttpContext.TraceIdentifier));
    }
}
