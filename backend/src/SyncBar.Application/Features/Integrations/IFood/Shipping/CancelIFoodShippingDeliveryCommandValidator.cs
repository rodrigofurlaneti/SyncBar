using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed class CancelIFoodShippingDeliveryCommandValidator : AbstractValidator<CancelIFoodShippingDeliveryCommand>
{
    public CancelIFoodShippingDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CancellationCode).GreaterThanOrEqualTo(0);
    }
}
