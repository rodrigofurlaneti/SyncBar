using FluentValidation;

namespace SyncBar.Application.Features.Finance.SetTarget;

public sealed class SetRevenueTargetCommandValidator : AbstractValidator<SetRevenueTargetCommand>
{
    public SetRevenueTargetCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ReferenceMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ReferenceYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
    }
}
