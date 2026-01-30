using FluentValidation;
using Atlas.Application.Models;

namespace Atlas.Application.Validators;

public sealed class AuthRefreshRequestValidator : AbstractValidator<AuthRefreshRequest>
{
    public AuthRefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}
