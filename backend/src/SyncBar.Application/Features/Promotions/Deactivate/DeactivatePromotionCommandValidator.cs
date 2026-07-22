using FluentValidation;

namespace SyncBar.Application.Features.Promotions.Deactivate;

public sealed class DeactivatePromotionCommandValidator : AbstractValidator<DeactivatePromotionCommand>
{
    public DeactivatePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).GreaterThan(0);
    }
}
