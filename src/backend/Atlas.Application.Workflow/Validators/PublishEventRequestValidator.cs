using Atlas.Application.Workflow.Models;
using FluentValidation;

namespace Atlas.Application.Workflow.Validators;

/// <summary>
/// 发布事件请求验证器
/// </summary>
public class PublishEventRequestValidator : AbstractValidator<PublishEventRequest>
{
    public PublishEventRequestValidator()
    {
        RuleFor(x => x.EventName)
            .NotEmpty()
            .WithMessage("Event name is required.");

        RuleFor(x => x.EventKey)
            .NotEmpty()
            .WithMessage("Event key is required.");
    }
}
