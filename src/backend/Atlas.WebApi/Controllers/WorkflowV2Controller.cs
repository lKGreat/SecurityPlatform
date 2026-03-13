using System.Text;
using System.Text.Json;
using Atlas.Application.Workflow.Abstractions.V2;
using Atlas.Application.Workflow.Models.V2;
using Atlas.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.WebApi.Controllers;

/// <summary>
/// Coze 风格工作流引擎 API（v2）
/// </summary>
[ApiController]
[Route("api/v2/workflows")]
[Authorize]
public sealed class WorkflowV2Controller : ControllerBase
{
    private readonly IWorkflowV2QueryService _queryService;
    private readonly IWorkflowV2CommandService _commandService;
    private readonly IWorkflowV2ExecutionService _executionService;

    public WorkflowV2Controller(
        IWorkflowV2QueryService queryService,
        IWorkflowV2CommandService commandService,
        IWorkflowV2ExecutionService executionService)
    {
        _queryService = queryService;
        _commandService = commandService;
        _executionService = executionService;
    }

    // =========== CRUD ===========

    /// <summary>创建工作流</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> CreateWorkflow(
        [FromBody] WorkflowCreateRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _commandService.CreateWorkflowAsync(request, cancellationToken);
        return Ok(ApiResponse<long>.Ok(id, HttpContext.TraceIdentifier));
    }

    /// <summary>工作流列表（分页）</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowListItem>>>> ListWorkflows(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.ListWorkflowsAsync(pageIndex, pageSize, keyword, cancellationToken);
        return Ok(ApiResponse<PagedResult<WorkflowListItem>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>获取工作流详情（含草稿画布）</summary>
    [HttpGet("{id:long}/canvas")]
    public async Task<ActionResult<ApiResponse<WorkflowDetailResponse>>> GetWorkflow(
        long id,
        CancellationToken cancellationToken)
    {
        var detail = await _queryService.GetWorkflowAsync(id, cancellationToken);
        if (detail is null) return NotFound(ApiResponse<WorkflowDetailResponse>.Fail("NOT_FOUND", "工作流不存在", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<WorkflowDetailResponse>.Ok(detail, HttpContext.TraceIdentifier));
    }

    /// <summary>保存草稿画布</summary>
    [HttpPut("{id:long}/draft")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveDraft(
        long id,
        [FromBody] WorkflowSaveRequest request,
        CancellationToken cancellationToken)
    {
        await _commandService.SaveDraftAsync(id, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>更新工作流元信息（名称/描述）</summary>
    [HttpPatch("{id:long}/meta")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateMeta(
        long id,
        [FromBody] WorkflowUpdateMetaRequest request,
        CancellationToken cancellationToken)
    {
        await _commandService.UpdateMetaAsync(id, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>发布工作流</summary>
    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<ApiResponse<WorkflowVersionItem>>> Publish(
        long id,
        [FromBody] WorkflowPublishRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _commandService.PublishAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowVersionItem>.Ok(version, HttpContext.TraceIdentifier));
    }

    /// <summary>复制工作流</summary>
    [HttpPost("{id:long}/copy")]
    public async Task<ActionResult<ApiResponse<long>>> Copy(
        long id,
        CancellationToken cancellationToken)
    {
        var newId = await _commandService.CopyWorkflowAsync(id, cancellationToken);
        return Ok(ApiResponse<long>.Ok(newId, HttpContext.TraceIdentifier));
    }

    /// <summary>删除工作流</summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        await _commandService.DeleteWorkflowAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>版本历史</summary>
    [HttpGet("{id:long}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowVersionItem>>>> ListVersions(
        long id,
        CancellationToken cancellationToken)
    {
        var versions = await _queryService.ListVersionsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkflowVersionItem>>.Ok(versions, HttpContext.TraceIdentifier));
    }

    /// <summary>节点类型列表</summary>
    [HttpGet("/api/v2/workflow-nodes/types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NodeTypeMetadata>>>> GetNodeTypes(
        CancellationToken cancellationToken)
    {
        var types = await _queryService.GetNodeTypesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NodeTypeMetadata>>.Ok(types, HttpContext.TraceIdentifier));
    }

    // =========== 执行 ===========

    /// <summary>同步执行工作流</summary>
    [HttpPost("{id:long}/run")]
    public async Task<ActionResult<ApiResponse<WorkflowRunResponse>>> SyncRun(
        long id,
        [FromBody] WorkflowRunRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _executionService.SyncRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowRunResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>异步执行工作流（返回 ExecutionId）</summary>
    [HttpPost("{id:long}/async-run")]
    public async Task<ActionResult<ApiResponse<long>>> AsyncRun(
        long id,
        [FromBody] WorkflowRunRequest request,
        CancellationToken cancellationToken)
    {
        var executionId = await _executionService.AsyncRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<long>.Ok(executionId, HttpContext.TraceIdentifier));
    }

    /// <summary>SSE 流式执行工作流</summary>
    [HttpGet("{id:long}/stream-run")]
    public async Task StreamRun(
        long id,
        [FromQuery] string? inputsJson = null,
        [FromQuery] string? version = null,
        CancellationToken cancellationToken = default)
    {
        var inputs = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(inputsJson))
        {
            try
            {
                inputs = JsonSerializer.Deserialize<Dictionary<string, object?>>(inputsJson) ?? inputs;
            }
            catch
            {
                // ignore invalid inputs JSON
            }
        }

        var request = new WorkflowRunRequest { Inputs = inputs, Version = version };

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var evt in _executionService.StreamRunAsync(id, request, cancellationToken))
        {
            var dataJson = JsonSerializer.Serialize(evt.Data);
            var sseFrame = $"event: {evt.EventType}\ndata: {dataJson}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(sseFrame), cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    // =========== 执行管理 ===========

    /// <summary>取消执行</summary>
    [HttpDelete("/api/v2/executions/{executionId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelExecution(
        long executionId,
        CancellationToken cancellationToken)
    {
        await _executionService.CancelAsync(executionId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>执行进度（节点列表 + 状态）</summary>
    [HttpGet("/api/v2/executions/{executionId:long}/process")]
    public async Task<ActionResult<ApiResponse<WorkflowProcessResponse>>> GetProcess(
        long executionId,
        CancellationToken cancellationToken)
    {
        var process = await _queryService.GetExecutionProcessAsync(executionId, cancellationToken);
        if (process is null) return NotFound(ApiResponse<WorkflowProcessResponse>.Fail("NOT_FOUND", "执行实例不存在", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<WorkflowProcessResponse>.Ok(process, HttpContext.TraceIdentifier));
    }

    /// <summary>节点执行详情（含输入/输出 JSON）</summary>
    [HttpGet("/api/v2/executions/{executionId:long}/nodes/{nodeKey}/history")]
    public async Task<ActionResult<ApiResponse<NodeExecutionDetailResponse>>> GetNodeHistory(
        long executionId,
        string nodeKey,
        CancellationToken cancellationToken)
    {
        var detail = await _queryService.GetNodeExecutionDetailAsync(executionId, nodeKey, cancellationToken);
        if (detail is null) return NotFound(ApiResponse<NodeExecutionDetailResponse>.Fail("NOT_FOUND", "节点执行记录不存在", HttpContext.TraceIdentifier));
        return Ok(ApiResponse<NodeExecutionDetailResponse>.Ok(detail, HttpContext.TraceIdentifier));
    }

    /// <summary>中断恢复（QuestionAnswer 等待用户输入后提交）</summary>
    [HttpPost("/api/v2/executions/{executionId:long}/resume")]
    public async Task<ActionResult<ApiResponse<bool>>> Resume(
        long executionId,
        [FromBody] WorkflowResumeRequest request,
        CancellationToken cancellationToken)
    {
        await _executionService.ResumeAsync(executionId, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>单节点调试</summary>
    [HttpPost("{id:long}/nodes/{nodeKey}/debug")]
    public async Task<ActionResult<ApiResponse<NodeDebugResponse>>> DebugNode(
        long id,
        string nodeKey,
        [FromBody] NodeDebugRequest request,
        CancellationToken cancellationToken)
    {
        request.NodeKey = nodeKey;
        var result = await _executionService.DebugNodeAsync(id, request, cancellationToken);
        return Ok(ApiResponse<NodeDebugResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
