using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed class MarkIfoodArrivedAtOriginCommandValidator : AbstractValidator<MarkIfoodArrivedAtOriginCommand>
{
    public MarkIfoodArrivedAtOriginCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
    }
}
