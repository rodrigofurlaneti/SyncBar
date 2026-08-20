using FluentValidation;

namespace SyncBar.Application.Features.Catalog.Complements.AddComplement;

public sealed class AddComplementCommandValidator : AbstractValidator<AddComplementCommand>
{
    public AddComplementCommandValidator()
    {
        RuleFor(x => x.ComplementGroupId).GreaterThan(0);
        RuleFor(x => x.ComplementItemId).GreaterThan(0);
        RuleFor(x => x.ExtraPrice).GreaterThanOrEqualTo(0);
    }
}
