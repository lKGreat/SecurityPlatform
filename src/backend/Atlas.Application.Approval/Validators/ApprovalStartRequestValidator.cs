using Atlas.Application.Approval.Models;
using FluentValidation;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 发起审批流程的验证器
/// </summary>
public sealed class ApprovalStartRequestValidator : AbstractValidator<ApprovalStartRequest>
{
    public ApprovalStartRequestValidator()
    {
        RuleFor(x => x.DefinitionId)
            .GreaterThan(0).WithMessage("流程定义ID必须大于0");

        RuleFor(x => x.BusinessKey)
            .NotEmpty().WithMessage("业务key不能为空")
            .MaximumLength(200).WithMessage("业务key长度不超过200个字符");

        RuleFor(x => x.DataJson)
            .MaximumLength(10000).WithMessage("业务数据JSON长度不超过10000个字符");
    }
}
