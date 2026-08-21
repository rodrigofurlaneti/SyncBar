using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class AssignIFoodDriverCommandValidator : AbstractValidator<AssignIFoodDriverCommand>
{
    public AssignIFoodDriverCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.DriverName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DriverPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.DriverVehicleType).NotEmpty().MaximumLength(30);
    }
}
