using FluentValidation;

namespace SyncBar.Application.Features.Orders.StartPreparation;

public sealed class StartOrderPreparationCommandValidator : AbstractValidator<StartOrderPreparationCommand>
{
    public StartOrderPreparationCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
    }
}
