using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// 节点执行器接口，每种 NodeType 对应一个实现。
/// </summary>
public interface INodeExecutor
{
    NodeType NodeType { get; }

    /// <summary>
    /// 执行节点，将输出写入 context.SetOutput()，返回下一步应执行的端口（"default" / "true" / "false" / "done" / "body"）。
    /// 返回 null 表示工作流应终止（Exit 节点）。
    /// </summary>
    Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context);
}

public sealed class NodeExecutorResult
{
    public static readonly NodeExecutorResult Default = new("default");
    public static readonly NodeExecutorResult Done = new(null);

    private NodeExecutorResult(string? nextPort)
    {
        NextPort = nextPort;
    }

    /// <summary>下一步的端口，null 表示结束</summary>
    public string? NextPort { get; }

    public static NodeExecutorResult Port(string port) => new(port);
}
