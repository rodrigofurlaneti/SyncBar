using FluentValidation;

namespace SyncBar.Application.Features.Orders.RemoveItemComplement;

public sealed class RemoveOrderItemComplementCommandValidator : AbstractValidator<RemoveOrderItemComplementCommand>
{
    public RemoveOrderItemComplementCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.OrderItemId).GreaterThan(0);
        RuleFor(x => x.OrderItemComplementId).GreaterThan(0);
    }
}
