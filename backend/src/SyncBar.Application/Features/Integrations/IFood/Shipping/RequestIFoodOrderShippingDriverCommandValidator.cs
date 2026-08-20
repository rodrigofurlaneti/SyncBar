using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed class RequestIFoodOrderShippingDriverCommandValidator : AbstractValidator<RequestIFoodOrderShippingDriverCommand>
{
    public RequestIFoodOrderShippingDriverCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.QuoteId).NotEmpty();
    }
}
