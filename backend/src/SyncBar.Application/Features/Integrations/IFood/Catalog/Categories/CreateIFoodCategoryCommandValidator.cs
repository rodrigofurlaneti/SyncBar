using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

public sealed class CreateIFoodCategoryCommandValidator : AbstractValidator<CreateIFoodCategoryCommand>
{
    public CreateIFoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CatalogId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
