using FluentValidation;
using Atlas.Domain.Audit.Entities;

namespace Atlas.Application.Audit.Validators;

public sealed class AuditRecordValidator : AbstractValidator<AuditRecord>
{
    public AuditRecordValidator()
    {
        RuleFor(x => x.Actor).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Action).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Result).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Target).MaximumLength(256);
        RuleFor(x => x.IpAddress).MaximumLength(64);
        RuleFor(x => x.UserAgent).MaximumLength(256);
    }
}
