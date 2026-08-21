using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class CancelIFoodOrderCommandValidator : AbstractValidator<CancelIFoodOrderCommand>
{
    public CancelIFoodOrderCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(20);
    }
}
