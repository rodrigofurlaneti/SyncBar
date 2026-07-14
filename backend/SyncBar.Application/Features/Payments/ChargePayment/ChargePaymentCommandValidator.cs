using FluentValidation;

namespace SyncBar.Application.Features.Payments.ChargePayment;

public sealed class ChargePaymentCommandValidator : AbstractValidator<ChargePaymentCommand>
{
    public ChargePaymentCommandValidator()
    {
        RuleFor(x => x.SaleId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CustomerDocument).MaximumLength(20);
    }
}
