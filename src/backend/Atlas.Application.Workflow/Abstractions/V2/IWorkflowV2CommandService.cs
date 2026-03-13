using Atlas.Application.Workflow.Models.V2;

namespace Atlas.Application.Workflow.Abstractions.V2;

/// <summary>
/// Coze 风格工作流命令服务（v2）：创建、保存、发布、删除、复制
/// </summary>
public interface IWorkflowV2CommandService
{
    Task<long> CreateWorkflowAsync(WorkflowCreateRequest request, CancellationToken cancellationToken);

    Task SaveDraftAsync(long workflowId, WorkflowSaveRequest request, CancellationToken cancellationToken);

    Task UpdateMetaAsync(long workflowId, WorkflowUpdateMetaRequest request, CancellationToken cancellationToken);

    Task<WorkflowVersionItem> PublishAsync(long workflowId, WorkflowPublishRequest request, CancellationToken cancellationToken);

    Task DeleteWorkflowAsync(long workflowId, CancellationToken cancellationToken);

    Task<long> CopyWorkflowAsync(long workflowId, CancellationToken cancellationToken);
}
