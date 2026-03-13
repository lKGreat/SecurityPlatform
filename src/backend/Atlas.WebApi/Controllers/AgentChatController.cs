using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using Atlas.Core.Identity;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.WebApi.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

[ApiController]
[Route("api/v1/agents/{agentId:long}/chat")]
public sealed class AgentChatController : ControllerBase
{
    private readonly IAgentChatService _agentChatService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IValidator<AgentChatRequest> _chatValidator;
    private readonly IValidator<AgentChatCancelRequest> _cancelValidator;

    public AgentChatController(
        IAgentChatService agentChatService,
        ITenantProvider tenantProvider,
        ICurrentUserAccessor currentUserAccessor,
        IValidator<AgentChatRequest> chatValidator,
        IValidator<AgentChatCancelRequest> cancelValidator)
    {
        _agentChatService = agentChatService;
        _tenantProvider = tenantProvider;
        _currentUserAccessor = currentUserAccessor;
        _chatValidator = chatValidator;
        _cancelValidator = cancelValidator;
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.AgentView)]
    public async Task<ActionResult<ApiResponse<AgentChatResponse>>> Chat(
        long agentId,
        [FromBody] AgentChatRequest request,
        CancellationToken cancellationToken)
    {
        _chatValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        var userId = _currentUserAccessor.GetCurrentUserOrThrow().UserId;
        var result = await _agentChatService.ChatAsync(tenantId, userId, agentId, request, cancellationToken);
        return Ok(ApiResponse<AgentChatResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("stream")]
    [Authorize(Policy = PermissionPolicies.AgentView)]
    public async Task ChatStream(
        long agentId,
        [FromBody] AgentChatRequest request,
        CancellationToken cancellationToken)
    {
        _chatValidator.ValidateAndThrow(request);
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var tenantId = _tenantProvider.GetTenantId();
        var userId = _currentUserAccessor.GetCurrentUserOrThrow().UserId;

        await foreach (var chunk in _agentChatService.ChatStreamAsync(tenantId, userId, agentId, request, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    [HttpPost("cancel")]
    [Authorize(Policy = PermissionPolicies.AgentView)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(
        long agentId,
        [FromBody] AgentChatCancelRequest request,
        CancellationToken cancellationToken)
    {
        _ = agentId;
        _cancelValidator.ValidateAndThrow(request);
        var tenantId = _tenantProvider.GetTenantId();
        await _agentChatService.CancelAsync(tenantId, request.ConversationId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Id = request.ConversationId.ToString() }, HttpContext.TraceIdentifier));
    }
}
