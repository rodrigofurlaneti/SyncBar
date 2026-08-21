using FluentValidation;

namespace SyncBar.Application.Features.Orders.AddItemComplement;

public sealed class AddOrderItemComplementCommandValidator : AbstractValidator<AddOrderItemComplementCommand>
{
    public AddOrderItemComplementCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.OrderItemId).GreaterThan(0);
        RuleFor(x => x.ComplementGroupId).GreaterThan(0);
        RuleFor(x => x.ComplementId).GreaterThan(0);
    }
}
