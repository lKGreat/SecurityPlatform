using Atlas.Application.LowCode.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace Atlas.Application.LowCode.Validators;

file static class LowCodeEnvironmentValidationHelpers
{
    public static bool BeValidVariablesJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class LowCodeEnvironmentCreateRequestValidator : AbstractValidator<LowCodeEnvironmentCreateRequest>
{
    public LowCodeEnvironmentCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodeEnvNameRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["LowCodeEnvNameMaxLength"].Value);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(localizer["LowCodeEnvCodeRequired"].Value)
            .MaximumLength(50).WithMessage(localizer["LowCodeEnvCodeMaxLength"].Value)
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$").WithMessage(localizer["LowCodeEnvCodeFormat"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(localizer["LowCodeEnvDescriptionMaxLength"].Value);

        RuleFor(x => x.VariablesJson)
            .Must(LowCodeEnvironmentValidationHelpers.BeValidVariablesJson)
            .WithMessage(localizer["LowCodeEnvVariablesJsonInvalid"].Value);
    }
}

public sealed class LowCodeEnvironmentUpdateRequestValidator : AbstractValidator<LowCodeEnvironmentUpdateRequest>
{
    public LowCodeEnvironmentUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["LowCodeEnvNameRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["LowCodeEnvNameMaxLength"].Value);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(localizer["LowCodeEnvDescriptionMaxLength"].Value);

        RuleFor(x => x.VariablesJson)
            .Must(LowCodeEnvironmentValidationHelpers.BeValidVariablesJson)
            .WithMessage(localizer["LowCodeEnvVariablesJsonInvalid"].Value);
    }
}
