using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class DispatchIFoodLogisticsCommandValidator : AbstractValidator<DispatchIFoodLogisticsCommand>
{
    public DispatchIFoodLogisticsCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
