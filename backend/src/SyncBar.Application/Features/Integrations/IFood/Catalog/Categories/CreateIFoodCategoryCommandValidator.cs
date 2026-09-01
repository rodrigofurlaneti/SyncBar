using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

public sealed class CreateIfoodCategoryCommandValidator : AbstractValidator<CreateIfoodCategoryCommand>
{
    public CreateIfoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CatalogId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
