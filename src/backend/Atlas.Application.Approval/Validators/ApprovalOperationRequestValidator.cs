using FluentValidation;
using Atlas.Application.Approval.Models;
using Atlas.Application.Resources;
using Atlas.Domain.Approval.Enums;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 审批流运行时操作请求验证器
/// </summary>
public sealed class ApprovalOperationRequestValidator : AbstractValidator<ApprovalOperationRequest>
{
    public ApprovalOperationRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.OperationType)
            .IsInEnum()
            .WithMessage(localizer["ApprovalOperationTypeInvalid"].Value);

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Comment))
            .WithMessage(localizer["ApprovalCommentMaxLength"].Value);

        RuleFor(x => x.TargetNodeId)
            .NotEmpty()
            .When(x => x.OperationType == ApprovalOperationType.BackToAnyNode)
            .WithMessage(localizer["ApprovalTargetNodeRequired"].Value);

        RuleFor(x => x.TargetAssigneeValue)
            .NotEmpty()
            .When(x => x.OperationType == ApprovalOperationType.Transfer
                || x.OperationType == ApprovalOperationType.ChangeAssignee)
            .WithMessage(localizer["ApprovalTransferTargetRequired"].Value);

        RuleFor(x => x.AdditionalAssigneeValues)
            .NotEmpty()
            .When(x => x.OperationType == ApprovalOperationType.AddAssignee)
            .WithMessage(localizer["ApprovalAddAssigneeRequired"].Value);
    }
}
