namespace Atlas.Application.Approval.Models;

/// <summary>
/// 创建审批流定义请求
/// </summary>
public record ApprovalFlowDefinitionCreateRequest
{
    /// <summary>流程名称</summary>
    public required string Name { get; init; }

    /// <summary>流程定义 JSON（节点、连线、配置等）</summary>
    public required string DefinitionJson { get; init; }
}
