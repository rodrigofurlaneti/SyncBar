using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed class CancelIfoodShippingDeliveryCommandValidator : AbstractValidator<CancelIfoodShippingDeliveryCommand>
{
    public CancelIfoodShippingDeliveryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CancellationCode).GreaterThanOrEqualTo(0);
    }
}
