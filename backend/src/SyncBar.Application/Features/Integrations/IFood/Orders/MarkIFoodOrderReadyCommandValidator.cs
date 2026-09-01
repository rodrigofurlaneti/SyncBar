using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed class MarkIfoodOrderReadyCommandValidator : AbstractValidator<MarkIfoodOrderReadyCommand>
{
    public MarkIfoodOrderReadyCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
    }
}
