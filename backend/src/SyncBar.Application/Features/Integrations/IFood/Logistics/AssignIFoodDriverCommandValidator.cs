using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed class AssignIfoodDriverCommandValidator : AbstractValidator<AssignIfoodDriverCommand>
{
    public AssignIfoodDriverCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
        RuleFor(x => x.DriverName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DriverPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.DriverVehicleType).NotEmpty().MaximumLength(30);
    }
}
