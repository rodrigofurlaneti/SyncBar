using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed class RequestIFoodShippingDriverCommandValidator : AbstractValidator<RequestIFoodShippingDriverCommand>
{
    public RequestIFoodShippingDriverCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OrderReference).MaximumLength(150);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CustomerPhoneAreaCode).NotEmpty().MaximumLength(5);
        RuleFor(x => x.CustomerPhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.MerchantFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(15);
        RuleFor(x => x.StreetNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.StreetName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Complement).MaximumLength(100);
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(2);
        RuleFor(x => x.Reference).MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Informe ao menos um item do pedido.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
