namespace Atlas.Application.Approval.Models;

/// <summary>
/// 更新审批流定义请求
/// </summary>
public record ApprovalFlowDefinitionUpdateRequest
{
    /// <summary>流程定义 ID</summary>
    public required long Id { get; init; }

    /// <summary>流程名称</summary>
    public required string Name { get; init; }

    /// <summary>流程定义 JSON</summary>
    public required string DefinitionJson { get; init; }
}
