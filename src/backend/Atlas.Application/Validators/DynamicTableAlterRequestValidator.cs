using System.Text.RegularExpressions;
using Atlas.Application.DynamicTables.Models;
using Atlas.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Validators;

public sealed class DynamicTableAlterRequestValidator : AbstractValidator<DynamicTableAlterRequest>
{
    private static readonly Regex FieldNamePattern = new("^[A-Za-z][A-Za-z0-9_]{1,63}$", RegexOptions.Compiled);

    public DynamicTableAlterRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x)
            .Must(request => request.AddFields.Count > 0 || request.UpdateFields.Count > 0 || request.RemoveFields.Count > 0)
            .WithMessage(localizer["DynamicAlterAtLeastOneChange"].Value);

        RuleForEach(x => x.RemoveFields)
            .Must(field => !string.IsNullOrWhiteSpace(field)
                           && FieldNamePattern.IsMatch(field)
                           && !field.Equals("TenantIdValue", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["DynamicAlterFieldNameInvalid"].Value);

        RuleFor(x => x.RemoveFields)
            .Must(HaveUniqueRemoveFieldNames)
            .WithMessage(localizer["DynamicAlterFieldNameDuplicated"].Value);

        RuleForEach(x => x.AddFields)
            .Must(field => !string.IsNullOrWhiteSpace(field.Name)
                           && FieldNamePattern.IsMatch(field.Name)
                           && !field.Name.Equals("TenantIdValue", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["DynamicAlterFieldNameInvalid"].Value);

        RuleFor(x => x.AddFields)
            .Must(HaveUniqueFieldNames)
            .WithMessage(localizer["DynamicAlterFieldNameDuplicated"].Value);

        RuleForEach(x => x.UpdateFields)
            .Must(field => !string.IsNullOrWhiteSpace(field.Name) && FieldNamePattern.IsMatch(field.Name))
            .WithMessage(localizer["DynamicAlterFieldNameInvalid"].Value);

        RuleFor(x => x.UpdateFields)
            .Must(HaveUniqueUpdateFieldNames)
            .WithMessage(localizer["DynamicAlterFieldNameDuplicated"].Value);

        RuleForEach(x => x.AddFields)
            .Must(ValidateFieldValidation)
            .WithMessage(localizer["DynamicAlterFieldNameInvalid"].Value);

        RuleForEach(x => x.UpdateFields)
            .Must(ValidateFieldValidation)
            .WithMessage(localizer["DynamicAlterFieldNameInvalid"].Value);
    }

    private static bool HaveUniqueFieldNames(IReadOnlyList<DynamicFieldDefinition> fields)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!set.Add(field.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveUniqueUpdateFieldNames(IReadOnlyList<DynamicFieldUpdateDefinition> fields)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!set.Add(field.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveUniqueRemoveFieldNames(IReadOnlyList<string> fields)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!set.Add(field))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateFieldValidation(DynamicFieldDefinition field)
    {
        return ValidateRule(field.Validation);
    }

    private static bool ValidateFieldValidation(DynamicFieldUpdateDefinition field)
    {
        return ValidateRule(field.Validation);
    }

    private static bool ValidateRule(DynamicFieldValidationDefinition? validation)
    {
        if (validation is null)
        {
            return true;
        }

        if (validation.MinLength.HasValue && validation.MinLength < 0)
        {
            return false;
        }

        if (validation.MaxLength.HasValue && validation.MaxLength < 0)
        {
            return false;
        }

        if (validation.MinLength.HasValue && validation.MaxLength.HasValue && validation.MinLength > validation.MaxLength)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(validation.Pattern))
        {
            try
            {
                _ = new Regex(validation.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return true;
    }
}
