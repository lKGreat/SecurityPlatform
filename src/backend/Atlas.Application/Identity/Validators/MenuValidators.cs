using FluentValidation;
using Atlas.Application.Identity.Models;

namespace Atlas.Application.Identity.Validators;

public sealed class MenuCreateRequestValidator : AbstractValidator<MenuCreateRequest>
{
    public MenuCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Path).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Component).MaximumLength(256).When(x => x.Component is not null);
        RuleFor(x => x.Icon).MaximumLength(64).When(x => x.Icon is not null);
        RuleFor(x => x.PermissionCode).MaximumLength(128).When(x => x.PermissionCode is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class MenuUpdateRequestValidator : AbstractValidator<MenuUpdateRequest>
{
    public MenuUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Path).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Component).MaximumLength(256).When(x => x.Component is not null);
        RuleFor(x => x.Icon).MaximumLength(64).When(x => x.Icon is not null);
        RuleFor(x => x.PermissionCode).MaximumLength(128).When(x => x.PermissionCode is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
