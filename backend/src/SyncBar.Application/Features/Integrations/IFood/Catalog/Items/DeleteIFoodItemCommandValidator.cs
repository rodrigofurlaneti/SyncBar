using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

public sealed class DeleteIFoodItemCommandValidator : AbstractValidator<DeleteIFoodItemCommand>
{
    public DeleteIFoodItemCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
