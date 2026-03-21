using System.Text.RegularExpressions;
using Atlas.Application.DynamicTables.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Validators;

public sealed class MigrationRecordCreateRequestValidator : AbstractValidator<MigrationRecordCreateRequest>
{
    private static readonly Regex TableKeyPattern = new("^[A-Za-z][A-Za-z0-9_]{1,63}$", RegexOptions.Compiled);

    public MigrationRecordCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.TableKey)
            .NotEmpty()
            .Must(v => TableKeyPattern.IsMatch(v))
            .WithMessage(localizer["DynamicTableTableKeyInvalid"].Value);

        RuleFor(x => x.Version)
            .GreaterThan(0);

        RuleFor(x => x.UpScript)
            .NotEmpty()
            .MaximumLength(100_000);

        RuleFor(x => x.DownScript)
            .MaximumLength(100_000)
            .When(x => x.DownScript is not null);
    }
}
