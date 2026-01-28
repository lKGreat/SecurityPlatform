using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Enums;
using FluentValidation;

namespace Atlas.WebApi.Controllers;

/// <summary>
/// 审批流运行时控制器（发起、我的发起、实例查询等）
/// </summary>
[ApiController]
[Route("api/approval/runtime")]
[Authorize]
public sealed class ApprovalRuntimeController : ControllerBase
{
    private readonly IApprovalRuntimeQueryService _queryService;
    private readonly IApprovalRuntimeCommandService _commandService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IValidator<ApprovalStartRequest> _startValidator;

    public ApprovalRuntimeController(
        IApprovalRuntimeQueryService queryService,
        IApprovalRuntimeCommandService commandService,
        ITenantProvider tenantProvider,
        IValidator<ApprovalStartRequest> startValidator)
    {
        _queryService = queryService;
        _commandService = commandService;
        _tenantProvider = tenantProvider;
        _startValidator = startValidator;
    }

    /// <summary>
    /// 发起流程实例
    /// </summary>
    [HttpPost("start")]
    public async Task<ApiResponse<ApprovalInstanceResponse>> StartAsync(
        ApprovalStartRequest request,
        CancellationToken cancellationToken = default)
    {
        await _startValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tenantId = _tenantProvider.GetTenantId();
        var userId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var result = await _commandService.StartAsync(tenantId, request, userId, cancellationToken);
        return ApiResponse<ApprovalInstanceResponse>.Ok(result, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// 获取我发起的流程实例
    /// </summary>
    [HttpGet("my-instances")]
    public async Task<ApiResponse<PagedResult<ApprovalInstanceListItem>>> GetMyInstancesAsync(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ApprovalInstanceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var request = new PagedRequest(pageIndex, pageSize, null, null, false);
        var result = await _queryService.GetInstancesByInitiatorAsync(tenantId, userId, request, status, cancellationToken);
        return ApiResponse<PagedResult<ApprovalInstanceListItem>>.Ok(result, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// 获取流程实例详情
    /// </summary>
    [HttpGet("instances/{id}")]
    public async Task<ApiResponse<ApprovalInstanceResponse>> GetInstanceByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var result = await _queryService.GetInstanceByIdAsync(tenantId, id, cancellationToken);
        if (result == null)
        {
            return ApiResponse<ApprovalInstanceResponse>.Fail(
                "NOT_FOUND",
                "流程实例不存在",
                HttpContext.TraceIdentifier);
        }

        return ApiResponse<ApprovalInstanceResponse>.Ok(result, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// 获取流程实例的历史事件
    /// </summary>
    [HttpGet("instances/{id}/history")]
    public async Task<ApiResponse<PagedResult<ApprovalHistoryEventResponse>>> GetHistoryAsync(
        long id,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var request = new PagedRequest(pageIndex, pageSize, null, null, false);
        var result = await _queryService.GetHistoryAsync(tenantId, id, request, cancellationToken);
        return ApiResponse<PagedResult<ApprovalHistoryEventResponse>>.Ok(result, HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// 取消流程实例
    /// </summary>
    [HttpPost("instances/{id}/cancel")]
    public async Task<ApiResponse<string>> CancelAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        await _commandService.CancelInstanceAsync(tenantId, id, userId, cancellationToken);
        return ApiResponse<string>.Ok("已取消", HttpContext.TraceIdentifier);
    }
}
