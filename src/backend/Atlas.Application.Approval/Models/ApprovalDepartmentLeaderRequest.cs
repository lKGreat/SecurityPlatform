namespace Atlas.Application.Approval.Models;

/// <summary>
/// 部门负责人映射请求
/// </summary>
public record ApprovalDepartmentLeaderRequest
{
    /// <summary>部门 ID</summary>
    public required long DepartmentId { get; init; }

    /// <summary>负责人用户 ID</summary>
    public required long LeaderUserId { get; init; }
}
