namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 工作流定义加载异常
/// </summary>
public class WorkflowDefinitionLoadException : Exception
{
    public WorkflowDefinitionLoadException()
    {
    }

    public WorkflowDefinitionLoadException(string message) : base(message)
    {
    }

    public WorkflowDefinitionLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
