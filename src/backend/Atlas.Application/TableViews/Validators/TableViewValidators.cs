using Atlas.Application.Resources;
using Atlas.Application.TableViews.Models;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.TableViews.Validators;

public sealed class TableViewCreateRequestValidator : AbstractValidator<TableViewCreateRequest>
{
    public TableViewCreateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.TableKey)
            .NotEmpty().WithMessage(localizer["TableViewKeyRequired"].Value)
            .MaximumLength(100).WithMessage(localizer["TableViewKeyMaxLength"].Value);
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["TableViewNameRequired"].Value)
            .MaximumLength(50).WithMessage(localizer["TableViewNameMaxLength"].Value);
        RuleFor(x => x.Config)
            .NotNull().WithMessage(localizer["TableViewConfigRequired"].Value);
    }
}

public sealed class TableViewUpdateRequestValidator : AbstractValidator<TableViewUpdateRequest>
{
    public TableViewUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["TableViewNameRequired"].Value)
            .MaximumLength(50).WithMessage(localizer["TableViewNameMaxLength"].Value);
        RuleFor(x => x.Config)
            .NotNull().WithMessage(localizer["TableViewConfigRequired"].Value);
    }
}

public sealed class TableViewConfigUpdateRequestValidator : AbstractValidator<TableViewConfigUpdateRequest>
{
    public TableViewConfigUpdateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Config)
            .NotNull().WithMessage(localizer["TableViewConfigRequired"].Value);
    }
}

public sealed class TableViewDuplicateRequestValidator : AbstractValidator<TableViewDuplicateRequest>
{
    public TableViewDuplicateRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["TableViewNameRequired"].Value)
            .MaximumLength(50).WithMessage(localizer["TableViewNameMaxLength"].Value);
    }
}
