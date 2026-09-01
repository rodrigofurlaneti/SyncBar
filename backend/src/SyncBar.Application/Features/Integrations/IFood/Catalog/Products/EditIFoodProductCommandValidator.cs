using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

public sealed class EditIfoodProductCommandValidator : AbstractValidator<EditIfoodProductCommand>
{
    public EditIfoodProductCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
