using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

public sealed class MarkIfoodGoingToOriginCommandValidator : AbstractValidator<MarkIfoodGoingToOriginCommand>
{
    public MarkIfoodGoingToOriginCommandValidator()
    {
        RuleFor(x => x.IfoodOrderId).GreaterThan(0);
    }
}
