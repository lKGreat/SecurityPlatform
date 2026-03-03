using Atlas.Application.LowCode.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.LowCode.Validators;

public sealed class LowCodeAppCreateRequestValidator : AbstractValidator<LowCodeAppCreateRequest>
{
    public LowCodeAppCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.AppKey)
            .NotEmpty().WithMessage(localizer["LowCodeAppKeyRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["LowCodeAppKeyMaxLength"].Value)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$").WithMessage(localizer["LowCodeAppKeyFormatInvalid"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodeAppNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["LowCodeAppNameMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["LowCodeAppDescriptionMaxLength"].Value);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage(localizer["LowCodeAppCategoryMaxLength"].Value);
    }
}

public sealed class LowCodeAppUpdateRequestValidator : AbstractValidator<LowCodeAppUpdateRequest>
{
    public LowCodeAppUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodeAppNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["LowCodeAppNameMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["LowCodeAppDescriptionMaxLength"].Value);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage(localizer["LowCodeAppCategoryMaxLength"].Value);
    }
}
