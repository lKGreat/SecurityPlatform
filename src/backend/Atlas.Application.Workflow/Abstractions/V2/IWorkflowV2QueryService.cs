using Atlas.Application.Workflow.Models.V2;
using Atlas.Core.Models;

namespace Atlas.Application.Workflow.Abstractions.V2;

/// <summary>
/// Coze 风格工作流查询服务（v2）
/// </summary>
public interface IWorkflowV2QueryService
{
    Task<PagedResult<WorkflowListItem>> ListWorkflowsAsync(int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken);

    Task<WorkflowDetailResponse?> GetWorkflowAsync(long workflowId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowVersionItem>> ListVersionsAsync(long workflowId, CancellationToken cancellationToken);

    Task<WorkflowProcessResponse?> GetExecutionProcessAsync(long executionId, CancellationToken cancellationToken);

    Task<NodeExecutionDetailResponse?> GetNodeExecutionDetailAsync(long executionId, string nodeKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<NodeTypeMetadata>> GetNodeTypesAsync(CancellationToken cancellationToken);
}
