using FluentValidation;

namespace SyncBar.Application.Features.Cash.CloseSession;

public sealed class CloseCashSessionCommandValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionCommandValidator()
    {
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.ClosedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.ClosingAmount).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.PaymentMethodCounts).ChildRules(count =>
        {
            count.RuleFor(x => x.PaymentMethodId).GreaterThan(0);
            count.RuleFor(x => x.CountedAmount).GreaterThanOrEqualTo(0);
        });
    }
}
