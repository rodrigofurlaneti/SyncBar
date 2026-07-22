using FluentValidation;

namespace SyncBar.Application.Features.Orders.Cancel;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
    }
}
