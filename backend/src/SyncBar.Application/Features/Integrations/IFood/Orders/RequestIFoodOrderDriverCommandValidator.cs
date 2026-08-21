using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class RequestIFoodOrderDriverCommandValidator : AbstractValidator<RequestIFoodOrderDriverCommand>
{
    public RequestIFoodOrderDriverCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
    }
}
