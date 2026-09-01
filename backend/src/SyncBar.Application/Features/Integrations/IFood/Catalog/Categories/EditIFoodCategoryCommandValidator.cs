using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

public sealed class EditIfoodCategoryCommandValidator : AbstractValidator<EditIfoodCategoryCommand>
{
    public EditIfoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CatalogId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
