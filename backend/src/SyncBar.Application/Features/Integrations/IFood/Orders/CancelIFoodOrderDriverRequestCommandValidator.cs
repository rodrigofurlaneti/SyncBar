using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class CancelIfoodOrderDriverRequestCommandValidator : AbstractValidator<CancelIfoodOrderDriverRequestCommand>
{
    public CancelIfoodOrderDriverRequestCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
    }
}
