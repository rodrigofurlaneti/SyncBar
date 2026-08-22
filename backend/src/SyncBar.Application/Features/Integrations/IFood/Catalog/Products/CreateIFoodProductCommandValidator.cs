using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

public sealed class CreateIFoodProductCommandValidator : AbstractValidator<CreateIFoodProductCommand>
{
    public CreateIFoodProductCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
