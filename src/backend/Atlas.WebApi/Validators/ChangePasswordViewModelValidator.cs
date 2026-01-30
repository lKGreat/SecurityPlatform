using FluentValidation;
using Atlas.Application.Options;
using Atlas.Application.Security;
using Atlas.WebApi.Models;
using Microsoft.Extensions.Options;

namespace Atlas.WebApi.Validators;

public sealed class ChangePasswordViewModelValidator : AbstractValidator<ChangePasswordViewModel>
{
    private readonly PasswordPolicyOptions _policy;

    public ChangePasswordViewModelValidator(IOptions<PasswordPolicyOptions> policyOptions)
    {
        _policy = policyOptions.Value;

        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ConfirmPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x).Must(x => x.NewPassword == x.ConfirmPassword).WithMessage("两次输入的新密码不一致");
        RuleFor(x => x.NewPassword).Custom((value, context) =>
        {
            if (!PasswordPolicy.IsCompliant(value, _policy, out var message))
            {
                context.AddFailure(message);
            }
        });
    }
}
