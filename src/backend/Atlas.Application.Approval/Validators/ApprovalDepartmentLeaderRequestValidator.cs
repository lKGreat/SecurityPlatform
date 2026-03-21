using Atlas.Application.Approval.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 部门负责人映射的验证器
/// </summary>
public sealed class ApprovalDepartmentLeaderRequestValidator : AbstractValidator<ApprovalDepartmentLeaderRequest>
{
    public ApprovalDepartmentLeaderRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage(localizer["ApprovalDeptIdRequired"].Value);

        RuleFor(x => x.LeaderUserId)
            .GreaterThan(0).WithMessage(localizer["ApprovalLeaderUserIdRequired"].Value);
    }
}
