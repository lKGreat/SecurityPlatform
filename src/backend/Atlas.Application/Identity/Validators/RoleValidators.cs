using FluentValidation;
using Atlas.Application.Identity.Models;

namespace Atlas.Application.Identity.Validators;

public sealed class RoleCreateRequestValidator : AbstractValidator<RoleCreateRequest>
{
    public RoleCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}

public sealed class RoleUpdateRequestValidator : AbstractValidator<RoleUpdateRequest>
{
    public RoleUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}
