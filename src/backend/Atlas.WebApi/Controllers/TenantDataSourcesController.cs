using Atlas.Application.Audit.Abstractions;
using Atlas.Application.Audit.Models;
using Atlas.Application.System.Abstractions;
using Atlas.Application.System.Models;
using Atlas.Core.Identity;
using Atlas.Core.Models;
using Atlas.WebApi.Authorization;
using Atlas.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

/// <summary>
/// 租户数据源管理（等保2.0 数据隔离）
/// </summary>
[ApiController]
[Route("api/v1/tenant-datasources")]
[Authorize(Policy = PermissionPolicies.SystemAdmin)]
public sealed class TenantDataSourcesController : ControllerBase
{
    private readonly ITenantDataSourceService _tenantDataSourceService;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IClientContextAccessor _clientContextAccessor;
    private readonly IAuditRecorder _auditRecorder;

    public TenantDataSourcesController(
        ITenantDataSourceService tenantDataSourceService,
        ICurrentUserAccessor currentUserAccessor,
        IClientContextAccessor clientContextAccessor,
        IAuditRecorder auditRecorder)
    {
        _tenantDataSourceService = tenantDataSourceService;
        _currentUserAccessor = currentUserAccessor;
        _clientContextAccessor = clientContextAccessor;
        _auditRecorder = auditRecorder;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantDataSourceDto>>>> GetAll(CancellationToken ct = default)
    {
        var dtos = await _tenantDataSourceService.QueryAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<TenantDataSourceDto>>.Ok(dtos, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] TenantDataSourceCreateRequest request,
        CancellationToken ct = default)
    {
        var id = await _tenantDataSourceService.CreateAsync(request, ct);

        await RecordAuditAsync("CREATE_DATASOURCE", request.TenantIdValue, ct);
        return Ok(ApiResponse<object>.Ok(new { Id = id.ToString() }, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        long id,
        [FromBody] TenantDataSourceUpdateRequest request,
        CancellationToken ct = default)
    {
        var updated = await _tenantDataSourceService.UpdateAsync(id, request, ct);
        if (!updated)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "数据源不存在", HttpContext.TraceIdentifier));
        }

        await RecordAuditAsync("UPDATE_DATASOURCE", id.ToString(), ct);
        return Ok(ApiResponse<object>.Ok(new { Id = id.ToString() }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct = default)
    {
        var deleted = await _tenantDataSourceService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "数据源不存在", HttpContext.TraceIdentifier));
        }

        await RecordAuditAsync("DELETE_DATASOURCE", id.ToString(), ct);
        return Ok(ApiResponse<object>.Ok(new { Id = id.ToString() }, HttpContext.TraceIdentifier));
    }

    [HttpPost("test")]
    public async Task<ActionResult<ApiResponse<TestConnectionResult>>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken ct = default)
    {
        var result = await _tenantDataSourceService.TestConnectionAsync(request, ct);
        return Ok(ApiResponse<TestConnectionResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    private async Task RecordAuditAsync(string action, string target, CancellationToken ct)
    {
        var currentUser = _currentUserAccessor.GetCurrentUser();
        if (currentUser is null) return;
        var actor = string.IsNullOrWhiteSpace(currentUser.Username)
            ? currentUser.UserId.ToString()
            : currentUser.Username;
        var auditContext = new AuditContext(
            currentUser.TenantId, actor, action, "SUCCESS", target,
            ControllerHelper.GetIpAddress(HttpContext),
            ControllerHelper.GetUserAgent(HttpContext),
            _clientContextAccessor.GetCurrent());
        await _auditRecorder.RecordAsync(auditContext, ct);
    }
}
