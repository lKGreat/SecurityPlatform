using Atlas.Application.LowCode.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.LowCode.Validators;

public sealed class FormDefinitionCreateRequestValidator : AbstractValidator<FormDefinitionCreateRequest>
{
    public FormDefinitionCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["FormNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["FormNameMaxLength"].Value);

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage(localizer["FormSchemaRequired"].Value);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage(localizer["FormCategoryMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["FormDescriptionMaxLength"].Value);
    }
}

public sealed class FormDefinitionUpdateRequestValidator : AbstractValidator<FormDefinitionUpdateRequest>
{
    public FormDefinitionUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["FormNameRequired"].Value)
            .MaximumLength(200).WithMessage(localizer["FormNameMaxLength"].Value);

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage(localizer["FormSchemaRequired"].Value);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage(localizer["FormCategoryMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(localizer["FormDescriptionMaxLength"].Value);
    }
}

public sealed class FormDefinitionSchemaUpdateRequestValidator : AbstractValidator<FormDefinitionSchemaUpdateRequest>
{
    public FormDefinitionSchemaUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage(localizer["FormSchemaRequired"].Value);
    }
}
