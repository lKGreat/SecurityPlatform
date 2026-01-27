using Atlas.Application.Identity.Abstractions;
using Atlas.Application.Identity.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

[ApiController]
[Route("departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentQueryService _departmentQueryService;
    private readonly IDepartmentCommandService _departmentCommandService;
    private readonly ITenantProvider _tenantProvider;
    private readonly Atlas.Core.Abstractions.IIdGenerator _idGenerator;
    private readonly IValidator<DepartmentCreateRequest> _createValidator;
    private readonly IValidator<DepartmentUpdateRequest> _updateValidator;

    public DepartmentsController(
        IDepartmentQueryService departmentQueryService,
        IDepartmentCommandService departmentCommandService,
        ITenantProvider tenantProvider,
        Atlas.Core.Abstractions.IIdGenerator idGenerator,
        IValidator<DepartmentCreateRequest> createValidator,
        IValidator<DepartmentUpdateRequest> updateValidator)
    {
        _departmentQueryService = departmentQueryService;
        _departmentCommandService = departmentCommandService;
        _tenantProvider = tenantProvider;
        _idGenerator = idGenerator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<DepartmentListItem>>>> Get(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var result = await _departmentQueryService.QueryDepartmentsAsync(request, tenantId, cancellationToken);
        return Ok(ApiResponse<PagedResult<DepartmentListItem>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentListItem>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var result = await _departmentQueryService.QueryAllAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DepartmentListItem>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] DepartmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        _createValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        var id = _idGenerator.NextId();
        var createdId = await _departmentCommandService.CreateAsync(tenantId, request, id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Id = createdId.ToString() }, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        long id,
        [FromBody] DepartmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        _updateValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        await _departmentCommandService.UpdateAsync(tenantId, id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Id = id.ToString() }, HttpContext.TraceIdentifier));
    }
}
