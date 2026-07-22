using FluentValidation;

namespace SyncBar.Application.Features.Orders.Reopen;

public sealed class ReopenOrderCommandValidator : AbstractValidator<ReopenOrderCommand>
{
    public ReopenOrderCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
    }
}
