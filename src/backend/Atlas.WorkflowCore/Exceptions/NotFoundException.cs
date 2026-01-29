namespace Atlas.WorkflowCore.Exceptions;

/// <summary>
/// 未找到异常
/// </summary>
public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} 未找到: {resourceId}")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string resourceType, string resourceId, Exception innerException)
        : base($"{resourceType} 未找到: {resourceId}", innerException)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
