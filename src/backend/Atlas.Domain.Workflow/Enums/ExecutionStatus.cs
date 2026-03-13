namespace Atlas.Domain.Workflow.Enums;

public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
    Cancelled = 4,
    Interrupted = 5
}
