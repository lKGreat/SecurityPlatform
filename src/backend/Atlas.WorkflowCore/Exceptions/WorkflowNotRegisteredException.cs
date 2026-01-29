namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 工作流未注册异常
/// </summary>
public class WorkflowNotRegisteredException : Exception
{
    public string WorkflowId { get; }
    public int? Version { get; }

    public WorkflowNotRegisteredException(string workflowId, int? version)
        : base($"工作流未注册: {workflowId} v{version}")
    {
        WorkflowId = workflowId;
        Version = version;
    }

    public WorkflowNotRegisteredException(string workflowId, int? version, Exception innerException)
        : base($"工作流未注册: {workflowId} v{version}", innerException)
    {
        WorkflowId = workflowId;
        Version = version;
    }
}
