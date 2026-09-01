using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed class RequestIfoodOrderShippingDriverCommandValidator : AbstractValidator<RequestIfoodOrderShippingDriverCommand>
{
    public RequestIfoodOrderShippingDriverCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
        RuleFor(x => x.QuoteId).NotEmpty();
    }
}
