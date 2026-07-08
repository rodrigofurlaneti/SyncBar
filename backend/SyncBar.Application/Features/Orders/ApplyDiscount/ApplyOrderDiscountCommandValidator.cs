using FluentValidation;

namespace SyncBar.Application.Features.Orders.ApplyDiscount;

public sealed class ApplyOrderDiscountCommandValidator : AbstractValidator<ApplyOrderDiscountCommand>
{
    public ApplyOrderDiscountCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}
