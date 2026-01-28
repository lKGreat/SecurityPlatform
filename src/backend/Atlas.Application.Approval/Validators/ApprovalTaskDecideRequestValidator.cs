using Atlas.Application.Approval.Models;
using FluentValidation;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 审批任务决策的验证器
/// </summary>
public sealed class ApprovalTaskDecideRequestValidator : AbstractValidator<ApprovalTaskDecideRequest>
{
    public ApprovalTaskDecideRequestValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("任务ID必须大于0");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("审批意见长度不超过500个字符");
    }
}
