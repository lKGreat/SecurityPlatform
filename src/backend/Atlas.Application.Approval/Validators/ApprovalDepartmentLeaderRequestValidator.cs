using Atlas.Application.Approval.Models;
using FluentValidation;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 部门负责人映射的验证器
/// </summary>
public sealed class ApprovalDepartmentLeaderRequestValidator : AbstractValidator<ApprovalDepartmentLeaderRequest>
{
    public ApprovalDepartmentLeaderRequestValidator()
    {
        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("部门ID必须大于0");

        RuleFor(x => x.LeaderUserId)
            .GreaterThan(0).WithMessage("用户ID必须大于0");
    }
}
