using FluentValidation;

namespace SyncBar.Application.Features.Cash.CloseSession;

public sealed class CloseCashSessionCommandValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionCommandValidator()
    {
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.ClosedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.ClosingAmount).GreaterThanOrEqualTo(0);
    }
}
