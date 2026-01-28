using Atlas.Application.Approval.Models;
using FluentValidation;

namespace Atlas.Application.Approval.Validators;

/// <summary>
/// 更新审批流定义的验证器
/// </summary>
public sealed class ApprovalFlowDefinitionUpdateRequestValidator : AbstractValidator<ApprovalFlowDefinitionUpdateRequest>
{
    public ApprovalFlowDefinitionUpdateRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("流程ID必须大于0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("流程名称不能为空")
            .MaximumLength(100).WithMessage("流程名称长度不超过100个字符");

        RuleFor(x => x.DefinitionJson)
            .NotEmpty().WithMessage("流程定义JSON不能为空");
    }
}
