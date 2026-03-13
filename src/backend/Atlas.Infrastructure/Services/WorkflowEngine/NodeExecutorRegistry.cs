using Atlas.Domain.Workflow.Enums;

namespace Atlas.Infrastructure.Services.WorkflowEngine;

/// <summary>
/// 节点执行器注册表，按 NodeType 索引执行器。
/// </summary>
public sealed class NodeExecutorRegistry
{
    private readonly Dictionary<NodeType, INodeExecutor> _executors;

    public NodeExecutorRegistry(IEnumerable<INodeExecutor> executors)
    {
        _executors = executors.ToDictionary(e => e.NodeType);
    }

    public INodeExecutor GetExecutor(NodeType nodeType)
    {
        if (_executors.TryGetValue(nodeType, out var executor))
            return executor;

        throw new InvalidOperationException($"未找到节点类型 {nodeType} 的执行器，请确保已注册对应的 INodeExecutor 实现。");
    }

    public bool TryGetExecutor(NodeType nodeType, out INodeExecutor? executor)
    {
        return _executors.TryGetValue(nodeType, out executor);
    }
}
