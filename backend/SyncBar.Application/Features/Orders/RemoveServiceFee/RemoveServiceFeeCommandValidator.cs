using FluentValidation;

namespace SyncBar.Application.Features.Orders.RemoveServiceFee;

public sealed class RemoveServiceFeeCommandValidator : AbstractValidator<RemoveServiceFeeCommand>
{
    public RemoveServiceFeeCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
    }
}
