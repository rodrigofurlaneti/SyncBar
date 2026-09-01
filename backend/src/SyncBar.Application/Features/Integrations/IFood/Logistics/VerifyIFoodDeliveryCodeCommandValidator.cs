using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed class VerifyIfoodDeliveryCodeCommandValidator : AbstractValidator<VerifyIfoodDeliveryCodeCommand>
{
    public VerifyIfoodDeliveryCodeCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}
