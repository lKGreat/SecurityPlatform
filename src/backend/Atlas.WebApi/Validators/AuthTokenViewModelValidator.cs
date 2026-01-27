using FluentValidation;
using Atlas.Application.Options;
using Atlas.Application.Security;
using Atlas.WebApi.Models;
using Microsoft.Extensions.Options;

namespace Atlas.WebApi.Validators;

public sealed class AuthTokenViewModelValidator : AbstractValidator<AuthTokenViewModel>
{
    private readonly PasswordPolicyOptions _policy;

    public AuthTokenViewModelValidator(IOptions<PasswordPolicyOptions> policyOptions)
    {
        _policy = policyOptions.Value;

        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).Custom((value, context) =>
        {
            if (!PasswordPolicy.IsCompliant(value, _policy, out var message))
            {
                context.AddFailure(message);
            }
        });
    }
}
