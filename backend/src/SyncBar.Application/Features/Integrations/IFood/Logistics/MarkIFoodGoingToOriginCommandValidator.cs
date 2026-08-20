using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class MarkIFoodGoingToOriginCommandValidator : AbstractValidator<MarkIFoodGoingToOriginCommand>
{
    public MarkIFoodGoingToOriginCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
