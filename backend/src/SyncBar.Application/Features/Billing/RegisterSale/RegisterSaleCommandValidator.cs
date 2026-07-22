using FluentValidation;

namespace SyncBar.Application.Features.Billing.RegisterSale;

public sealed class RegisterSaleCommandValidator : AbstractValidator<RegisterSaleCommand>
{
    public RegisterSaleCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Payments).NotEmpty().WithMessage("Sale requires at least one payment.");
        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.PaymentMethodId).GreaterThan(0);
            payment.RuleFor(p => p.Amount).GreaterThan(0);
            payment.RuleFor(p => p.ChangeAmount).GreaterThanOrEqualTo(0).When(p => p.ChangeAmount.HasValue);
            payment.RuleFor(p => p.AuthorizationCode).MaximumLength(100);
        });
    }
}
