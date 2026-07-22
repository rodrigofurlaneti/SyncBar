using FluentValidation;

namespace SyncBar.Application.Features.Billing.RefundSale;

public sealed class RefundSaleCommandValidator : AbstractValidator<RefundSaleCommand>
{
    public RefundSaleCommandValidator()
    {
        RuleFor(x => x.SaleId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(300);
    }
}
