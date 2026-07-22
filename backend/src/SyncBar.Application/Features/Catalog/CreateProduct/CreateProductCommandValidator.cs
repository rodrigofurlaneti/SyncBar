using FluentValidation;

namespace SyncBar.Application.Features.Catalog.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasureId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Barcode).MaximumLength(50);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).When(x => x.CostPrice.HasValue);
        RuleFor(x => x.PreparationTimeMinutes).GreaterThan(0).When(x => x.PreparationTimeMinutes.HasValue);
    }
}
