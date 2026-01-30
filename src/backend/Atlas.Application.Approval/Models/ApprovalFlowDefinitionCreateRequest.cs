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

    /// <summary>流程描述/说明</summary>
    public string? Description { get; init; }

    /// <summary>流程分类</summary>
    public string? Category { get; init; }

    /// <summary>可见范围配置 JSON</summary>
    public string? VisibilityScopeJson { get; init; }

    /// <summary>是否为快捷入口</summary>
    public bool IsQuickEntry { get; init; }
}
