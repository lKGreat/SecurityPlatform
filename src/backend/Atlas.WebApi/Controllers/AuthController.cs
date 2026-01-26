using Atlas.Application.Abstractions;
using Atlas.Application.Models;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthTokenService _authTokenService;
    private readonly ITenantProvider _tenantProvider;

    public AuthController(IAuthTokenService authTokenService, ITenantProvider tenantProvider)
    {
        _authTokenService = authTokenService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<AuthTokenResult>> CreateToken([FromBody] AuthTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessException("用户名或密码不能为空", ErrorCodes.ValidationError);
        }

        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId.IsEmpty)
        {
            throw new BusinessException("缺少租户标识", ErrorCodes.ValidationError);
        }

        var result = _authTokenService.CreateToken(request, tenantId);
        var payload = ApiResponse<AuthTokenResult>.Ok(result, HttpContext.TraceIdentifier);
        return Ok(payload);
    }
}