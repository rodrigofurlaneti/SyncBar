using FluentValidation;

namespace SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;

public sealed class UpdateComplementItemCommandValidator : AbstractValidator<UpdateComplementItemCommand>
{
    public UpdateComplementItemCommandValidator()
    {
        RuleFor(x => x.ComplementItemId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
