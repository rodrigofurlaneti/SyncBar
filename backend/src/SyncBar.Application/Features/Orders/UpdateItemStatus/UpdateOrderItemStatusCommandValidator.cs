using FluentValidation;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

public sealed class UpdateOrderItemStatusCommandValidator : AbstractValidator<UpdateOrderItemStatusCommand>
{
    public UpdateOrderItemStatusCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.OrderItemId).GreaterThan(0);
        RuleFor(x => x.OrderItemStatusId)
            .InclusiveBetween(OrderItemStatusIds.Lancado, OrderItemStatusIds.Cancelado);
    }
}
