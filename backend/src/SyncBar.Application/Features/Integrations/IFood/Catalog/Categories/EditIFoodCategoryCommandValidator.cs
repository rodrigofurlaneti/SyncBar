using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

public sealed class EditIFoodCategoryCommandValidator : AbstractValidator<EditIFoodCategoryCommand>
{
    public EditIFoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CatalogId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
