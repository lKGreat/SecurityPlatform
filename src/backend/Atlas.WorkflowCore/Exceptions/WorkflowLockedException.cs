namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 工作流锁定异常
/// </summary>
public class WorkflowLockedException : Exception
{
    public string WorkflowId { get; }

    public WorkflowLockedException(string workflowId)
        : base($"工作流已被锁定: {workflowId}")
    {
        WorkflowId = workflowId;
    }

    public WorkflowLockedException(string workflowId, Exception innerException)
        : base($"工作流已被锁定: {workflowId}", innerException)
    {
        WorkflowId = workflowId;
    }
}
