namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 活动失败异常
/// </summary>
public class ActivityFailedException : Exception
{
    public ActivityFailedException()
    {
    }

    public ActivityFailedException(string message) : base(message)
    {
    }

    public ActivityFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
