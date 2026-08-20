using FluentValidation;

namespace SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;

public sealed class CreateComplementItemCommandValidator : AbstractValidator<CreateComplementItemCommand>
{
    public CreateComplementItemCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
