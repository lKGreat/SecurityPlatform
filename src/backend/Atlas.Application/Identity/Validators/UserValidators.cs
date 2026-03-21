using FluentValidation;
using Microsoft.Extensions.Options;
using Atlas.Application.Identity.Models;
using Atlas.Application.Options;
using Atlas.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Atlas.Application.Identity.Validators;

public sealed class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
{
    public UserCreateRequestValidator(IOptions<PasswordPolicyOptions> policyOptions, IStringLocalizer<Messages> localizer)
    {
        var policy = policyOptions.Value;

        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(64)
            .Matches(@"^\S+$").WithMessage(localizer["UsernameNoWhitespace"].Value)
            .Must(username => !long.TryParse(username, out _)).WithMessage(localizer["UsernameNotNumeric"].Value);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256).When(x => x.Email is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(policy.MinLength);

        if (policy.RequireUppercase)
        {
            RuleFor(x => x.Password).Matches("[A-Z]").WithMessage(localizer["PasswordRequireUppercase"].Value);
        }

        if (policy.RequireLowercase)
        {
            RuleFor(x => x.Password).Matches("[a-z]").WithMessage(localizer["PasswordRequireLowercase"].Value);
        }

        if (policy.RequireDigit)
        {
            RuleFor(x => x.Password).Matches("[0-9]").WithMessage(localizer["PasswordRequireDigit"].Value);
        }

        if (policy.RequireNonAlphanumeric)
        {
            RuleFor(x => x.Password).Matches("[^a-zA-Z0-9]").WithMessage(localizer["PasswordRequireNonAlphanumeric"].Value);
        }
    }
}

public sealed class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256).When(x => x.Email is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => x.PhoneNumber is not null);
    }
}
