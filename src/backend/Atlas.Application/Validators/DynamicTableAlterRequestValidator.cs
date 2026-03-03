using System.Text.RegularExpressions;
using Atlas.Application.DynamicTables.Models;
using FluentValidation;

namespace Atlas.Application.Validators;

public sealed class DynamicTableAlterRequestValidator : AbstractValidator<DynamicTableAlterRequest>
{
    private static readonly Regex FieldNamePattern = new("^[A-Za-z][A-Za-z0-9_]{1,63}$", RegexOptions.Compiled);

    public DynamicTableAlterRequestValidator()
    {
        RuleFor(x => x)
            .Must(request => request.AddFields.Count > 0 || request.UpdateFields.Count > 0 || request.RemoveFields.Count > 0)
            .WithMessage("至少需要一项表结构变更。");

        RuleFor(x => x.UpdateFields)
            .Must(fields => fields.Count == 0)
            .WithMessage("当前版本暂不支持字段更新。");

        RuleFor(x => x.RemoveFields)
            .Must(fields => fields.Count == 0)
            .WithMessage("当前版本暂不支持字段删除。");

        RuleForEach(x => x.AddFields)
            .Must(field => !string.IsNullOrWhiteSpace(field.Name)
                           && FieldNamePattern.IsMatch(field.Name)
                           && !field.Name.Equals("TenantIdValue", StringComparison.OrdinalIgnoreCase))
            .WithMessage("新增字段名不合法。");

        RuleFor(x => x.AddFields)
            .Must(HaveUniqueFieldNames)
            .WithMessage("新增字段名不能重复。");
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
}
