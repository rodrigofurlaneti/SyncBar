using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class MarkIFoodArrivedAtDestinationCommandValidator : AbstractValidator<MarkIFoodArrivedAtDestinationCommand>
{
    public MarkIFoodArrivedAtDestinationCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
