using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class MarkIFoodArrivedAtOriginCommandValidator : AbstractValidator<MarkIFoodArrivedAtOriginCommand>
{
    public MarkIFoodArrivedAtOriginCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
