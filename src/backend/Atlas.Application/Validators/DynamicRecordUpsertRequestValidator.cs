using System.Text.RegularExpressions;
using Atlas.Application.DynamicTables.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Validators;

public sealed class DynamicRecordUpsertRequestValidator : AbstractValidator<DynamicRecordUpsertRequest>
{
    private static readonly Regex FieldPattern = new("^[A-Za-z][A-Za-z0-9_]{0,63}$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "String", "Int", "Long", "Decimal", "Bool", "DateTime", "Date"
    };

    public DynamicRecordUpsertRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Values)
            .NotNull()
            .Must(values => values.Count > 0)
            .WithMessage(localizer["DynamicRecordValuesRequired"].Value);

        RuleForEach(x => x.Values)
            .Must(value => !string.IsNullOrWhiteSpace(value.Field))
            .WithMessage(localizer["DynamicRecordFieldNameRequired"].Value);

        RuleForEach(x => x.Values)
            .Must(value => FieldPattern.IsMatch(value.Field))
            .WithMessage(localizer["DynamicRecordFieldNameFormat"].Value);

        RuleForEach(x => x.Values)
            .Must(value => AllowedValueTypes.Contains(value.ValueType))
            .WithMessage(localizer["DynamicRecordFieldValueTypeInvalid"].Value);

        RuleForEach(x => x.Values)
            .Must(HasSingleValue)
            .WithMessage(localizer["DynamicRecordFieldValueSingle"].Value);

        RuleFor(x => x.Values)
            .Must(HaveUniqueFields)
            .WithMessage(localizer["DynamicRecordFieldNameDuplicated"].Value);
    }

    private static bool HasSingleValue(DynamicFieldValueDto value)
    {
        var count = 0;
        if (value.StringValue is not null) count++;
        if (value.IntValue.HasValue) count++;
        if (value.LongValue.HasValue) count++;
        if (value.DecimalValue.HasValue) count++;
        if (value.BoolValue.HasValue) count++;
        if (value.DateTimeValue.HasValue) count++;
        if (value.DateValue.HasValue) count++;
        return count == 1;
    }

    private static bool HaveUniqueFields(IReadOnlyList<DynamicFieldValueDto> values)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (!set.Add(value.Field))
            {
                return false;
            }
        }

        return true;
    }
}
