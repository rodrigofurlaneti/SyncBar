using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed class ValidateIFoodPickupCodeCommandValidator : AbstractValidator<ValidateIFoodPickupCodeCommand>
{
    public ValidateIFoodPickupCodeCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}
