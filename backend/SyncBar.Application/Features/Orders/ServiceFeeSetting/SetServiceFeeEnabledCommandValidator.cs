using FluentValidation;

namespace SyncBar.Application.Features.Orders.ServiceFeeSetting;

public sealed class SetServiceFeeEnabledCommandValidator : AbstractValidator<SetServiceFeeEnabledCommand>
{
    public SetServiceFeeEnabledCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
