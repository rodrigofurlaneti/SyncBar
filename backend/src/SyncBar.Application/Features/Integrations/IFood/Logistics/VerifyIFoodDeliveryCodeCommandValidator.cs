using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

public sealed class VerifyIFoodDeliveryCodeCommandValidator : AbstractValidator<VerifyIFoodDeliveryCodeCommand>
{
    public VerifyIFoodDeliveryCodeCommandValidator()
    {
        RuleFor(x => x.IFoodOrderId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}
