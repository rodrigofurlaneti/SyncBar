using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class CancelIFoodOrderDriverRequestCommandValidator : AbstractValidator<CancelIFoodOrderDriverRequestCommand>
{
    public CancelIFoodOrderDriverRequestCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
