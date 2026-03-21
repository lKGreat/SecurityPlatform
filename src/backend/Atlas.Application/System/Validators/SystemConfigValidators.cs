using FluentValidation;
using Atlas.Application.System.Models;
using Atlas.Application.Resources;
using Microsoft.Extensions.Localization;
using global::System.Text.RegularExpressions;

namespace Atlas.Application.System.Validators;

public sealed class SystemConfigCreateRequestValidator : AbstractValidator<SystemConfigCreateRequest>
{
    private static readonly Regex SafeKeyPattern =
        new(@"^[a-zA-Z][a-zA-Z0-9_.]{0,127}$", RegexOptions.Compiled);

    public SystemConfigCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.ConfigKey)
            .NotEmpty()
            .MaximumLength(128)
            .Must(k => SafeKeyPattern.IsMatch(k))
            .WithMessage(localizer["SystemConfigKeyFormat"].Value);
        RuleFor(x => x.ConfigValue).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ConfigName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Remark).MaximumLength(500).When(x => x.Remark != null);
    }
}

public sealed class SystemConfigUpdateRequestValidator : AbstractValidator<SystemConfigUpdateRequest>
{
    public SystemConfigUpdateRequestValidator()
    {
        RuleFor(x => x.ConfigValue).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ConfigName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Remark).MaximumLength(500).When(x => x.Remark != null);
    }
}
