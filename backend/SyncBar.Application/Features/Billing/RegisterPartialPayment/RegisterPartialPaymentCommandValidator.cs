using FluentValidation;

namespace SyncBar.Application.Features.Billing.RegisterPartialPayment;

public sealed class RegisterPartialPaymentCommandValidator : AbstractValidator<RegisterPartialPaymentCommand>
{
    public RegisterPartialPaymentCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.PaymentMethodId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.AuthorizationCode).MaximumLength(100);
        RuleFor(x => x.PayerName).MaximumLength(100);
    }
}
