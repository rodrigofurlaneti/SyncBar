using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

public sealed class BatchUpdateIFoodProductPricesCommandValidator : AbstractValidator<BatchUpdateIFoodProductPricesCommand>
{
    public BatchUpdateIFoodProductPricesCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.Value).GreaterThanOrEqualTo(0));
    }
}
