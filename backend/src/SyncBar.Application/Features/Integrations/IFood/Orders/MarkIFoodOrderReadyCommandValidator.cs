using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class MarkIFoodOrderReadyCommandValidator : AbstractValidator<MarkIFoodOrderReadyCommand>
{
    public MarkIFoodOrderReadyCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
