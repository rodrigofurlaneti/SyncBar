using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

public sealed class EditIFoodProductCommandValidator : AbstractValidator<EditIFoodProductCommand>
{
    public EditIFoodProductCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
