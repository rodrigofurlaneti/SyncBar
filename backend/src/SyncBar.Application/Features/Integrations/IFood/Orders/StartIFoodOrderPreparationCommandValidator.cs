using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class StartIFoodOrderPreparationCommandValidator : AbstractValidator<StartIFoodOrderPreparationCommand>
{
    public StartIFoodOrderPreparationCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
