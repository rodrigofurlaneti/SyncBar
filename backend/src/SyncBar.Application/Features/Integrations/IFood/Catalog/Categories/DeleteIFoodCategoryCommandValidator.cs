using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

public sealed class DeleteIfoodCategoryCommandValidator : AbstractValidator<DeleteIfoodCategoryCommand>
{
    public DeleteIfoodCategoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
