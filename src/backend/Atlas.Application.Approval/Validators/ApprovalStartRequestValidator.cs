using Atlas.Application.Approval.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 发起审批流程的验证器
/// </summary>
public sealed class ApprovalStartRequestValidator : AbstractValidator<ApprovalStartRequest>
{
    public ApprovalStartRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.DefinitionId)
            .GreaterThan(0).WithMessage(localizer["ApprovalDefinitionIdRequired"].Value);

        RuleFor(x => x.BusinessKey)
            .NotEmpty().WithMessage(localizer["ApprovalBusinessKeyRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["ApprovalBusinessKeyMaxLength"].Value);

        RuleFor(x => x.DataJson)
            .MaximumLength(10000).WithMessage(localizer["ApprovalDataJsonMaxLength"].Value);
    }
}
