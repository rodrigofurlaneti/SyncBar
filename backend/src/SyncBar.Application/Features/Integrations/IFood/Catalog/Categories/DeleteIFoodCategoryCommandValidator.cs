using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

public sealed class DeleteIFoodCategoryCommandValidator : AbstractValidator<DeleteIFoodCategoryCommand>
{
    public DeleteIFoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
