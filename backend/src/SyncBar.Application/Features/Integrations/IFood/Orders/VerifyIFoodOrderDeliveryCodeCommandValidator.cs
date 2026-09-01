using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class VerifyIfoodOrderDeliveryCodeCommandValidator : AbstractValidator<VerifyIfoodOrderDeliveryCodeCommand>
{
    public VerifyIfoodOrderDeliveryCodeCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}
