using AutoMapper;
using FluentValidation;
using Atlas.Application.Abstractions;
using Atlas.Application.Audit.Abstractions;
using Atlas.Application.Models;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Audit.Entities;
using Atlas.WebApi.Helpers;
using Atlas.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthTokenService _authTokenService;
    private readonly IAuthProfileService _authProfileService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;
    private readonly IValidator<AuthTokenRequest> _validator;
    private readonly IAuditWriter _auditWriter;

    public AuthController(
        IAuthTokenService authTokenService,
        IAuthProfileService authProfileService,
        ITenantProvider tenantProvider,
        IMapper mapper,
        IValidator<AuthTokenRequest> validator,
        IAuditWriter auditWriter)
    {
        _authTokenService = authTokenService;
        _authProfileService = authProfileService;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
        _validator = validator;
        _auditWriter = auditWriter;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokenResult>>> CreateToken(
        [FromBody] AuthTokenViewModel request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId.IsEmpty)
        {
            throw new BusinessException("缺少租户标识", ErrorCodes.ValidationError);
        }

        var dto = _mapper.Map<AuthTokenRequest>(request);
        _validator.ValidateAndThrow(dto);

        var context = new AuthRequestContext(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        var result = await _authTokenService.CreateTokenAsync(dto, tenantId, context, cancellationToken);
        var payload = ApiResponse<AuthTokenResult>.Ok(result, HttpContext.TraceIdentifier);
        return Ok(payload);
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthTokenResult>>> RefreshToken(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = ControllerHelper.GetUserIdOrThrow(User);
        var context = new AuthRequestContext(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        var result = await _authTokenService.CreateTokenForUserAsync(userId, tenantId, context, cancellationToken);
        return Ok(ApiResponse<AuthTokenResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthProfileResult>>> Me(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = ControllerHelper.GetUserIdOrThrow(User);
        var profile = await _authProfileService.GetProfileAsync(userId, tenantId, cancellationToken);
        if (profile is null)
        {
            return NotFound(ApiResponse<AuthProfileResult>.Fail(ErrorCodes.NotFound, "用户不存在", HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<AuthProfileResult>.Ok(profile, HttpContext.TraceIdentifier));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = ControllerHelper.GetUserIdOrThrow(User);
        var actor = User.Identity?.Name ?? userId.ToString();

        var auditRecord = new AuditRecord(
            tenantId: tenantId,
            actor: actor,
            action: "LOGOUT",
            result: "SUCCESS",
            target: null,
            ipAddress: ControllerHelper.GetIpAddress(HttpContext),
            userAgent: ControllerHelper.GetUserAgent(HttpContext));

        await _auditWriter.WriteAsync(auditRecord, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Success = true }, HttpContext.TraceIdentifier));
    }
}
