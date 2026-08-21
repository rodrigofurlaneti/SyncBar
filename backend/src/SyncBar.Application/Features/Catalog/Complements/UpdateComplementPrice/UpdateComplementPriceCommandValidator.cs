using FluentValidation;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;

public sealed class UpdateComplementPriceCommandValidator : AbstractValidator<UpdateComplementPriceCommand>
{
    public UpdateComplementPriceCommandValidator()
    {
        RuleFor(x => x.ComplementGroupId).GreaterThan(0);
        RuleFor(x => x.ComplementId).GreaterThan(0);
        RuleFor(x => x.ExtraPrice).GreaterThanOrEqualTo(0);
    }
}
