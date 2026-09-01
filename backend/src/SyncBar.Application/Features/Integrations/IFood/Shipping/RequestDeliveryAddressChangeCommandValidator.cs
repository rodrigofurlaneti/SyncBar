using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed class RequestDeliveryAddressChangeCommandValidator : AbstractValidator<RequestDeliveryAddressChangeCommand>
{
    public RequestDeliveryAddressChangeCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
        RuleFor(x => x.StreetNumber).NotEmpty();
        RuleFor(x => x.StreetName).NotEmpty();
        RuleFor(x => x.Neighborhood).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
    }
}
