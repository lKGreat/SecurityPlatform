namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 持久化数据损坏异常
/// </summary>
public class CorruptPersistenceDataException : Exception
{
    public CorruptPersistenceDataException()
    {
    }

    public CorruptPersistenceDataException(string message) : base(message)
    {
    }

    public CorruptPersistenceDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
