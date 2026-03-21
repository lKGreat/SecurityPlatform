using Atlas.Application.LowCode.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.LowCode.Validators;

public sealed class LowCodePageCreateRequestValidator : AbstractValidator<LowCodePageCreateRequest>
{
    public LowCodePageCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.PageKey)
            .NotEmpty().WithMessage(localizer["LowCodePageKeyRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["LowCodePageKeyMaxLength"].Value)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$").WithMessage(localizer["LowCodePageKeyFormat"].Value);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodePageNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["LowCodePageNameMaxLength"].Value);

        RuleFor(x => x.PageType)
            .NotEmpty().WithMessage(localizer["LowCodePageTypeRequired"].Value);

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage(localizer["LowCodePageSchemaRequired"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["LowCodePageDescriptionMaxLength"].Value);
    }
}

public sealed class LowCodePageUpdateRequestValidator : AbstractValidator<LowCodePageUpdateRequest>
{
    public LowCodePageUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodePageNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["LowCodePageNameMaxLength"].Value);

        RuleFor(x => x.PageType)
            .NotEmpty().WithMessage(localizer["LowCodePageTypeRequired"].Value);

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage(localizer["LowCodePageSchemaRequired"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["LowCodePageDescriptionMaxLength"].Value);
    }
}
