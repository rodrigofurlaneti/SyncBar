using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

public sealed class DeleteIFoodProductCommandValidator : AbstractValidator<DeleteIFoodProductCommand>
{
    public DeleteIFoodProductCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
