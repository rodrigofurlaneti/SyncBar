using FluentValidation;

namespace SyncBar.Application.Features.Orders.AddPizzaItem;

public sealed class AddPizzaOrderItemCommandValidator : AbstractValidator<AddPizzaOrderItemCommand>
{
    public AddPizzaOrderItemCommandValidator()
    {
        RuleFor(x => x.CustomerOrderId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(300);
        RuleFor(x => x.PizzaSizeId).GreaterThan(0);
        RuleFor(x => x.PizzaCrustId).GreaterThan(0).When(x => x.PizzaCrustId.HasValue);
        RuleFor(x => x.PizzaEdgeId).GreaterThan(0).When(x => x.PizzaEdgeId.HasValue);
        RuleFor(x => x.PizzaFlavorIds).NotEmpty();
        RuleForEach(x => x.PizzaFlavorIds).GreaterThan(0);
    }
}
