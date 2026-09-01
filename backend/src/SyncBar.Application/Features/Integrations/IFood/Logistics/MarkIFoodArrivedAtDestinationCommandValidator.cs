using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed class MarkIfoodArrivedAtDestinationCommandValidator : AbstractValidator<MarkIfoodArrivedAtDestinationCommand>
{
    public MarkIfoodArrivedAtDestinationCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
    }
}
