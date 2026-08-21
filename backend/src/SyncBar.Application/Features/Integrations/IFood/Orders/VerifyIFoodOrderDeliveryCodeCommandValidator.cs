using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class VerifyIFoodOrderDeliveryCodeCommandValidator : AbstractValidator<VerifyIFoodOrderDeliveryCodeCommand>
{
    public VerifyIFoodOrderDeliveryCodeCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}
