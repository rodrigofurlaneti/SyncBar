using FluentValidation;

namespace SyncBar.Application.Features.Finance.DeactivateCost;

public sealed class DeactivateOperatingCostCommandValidator : AbstractValidator<DeactivateOperatingCostCommand>
{
    public DeactivateOperatingCostCommandValidator()
    {
        RuleFor(x => x.OperatingCostId).GreaterThan(0);
    }
}
