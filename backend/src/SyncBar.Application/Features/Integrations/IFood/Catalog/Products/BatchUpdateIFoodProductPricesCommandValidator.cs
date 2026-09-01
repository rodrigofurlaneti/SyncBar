using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

public sealed class BatchUpdateIfoodProductPricesCommandValidator : AbstractValidator<BatchUpdateIfoodProductPricesCommand>
{
    public BatchUpdateIfoodProductPricesCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.Value).GreaterThanOrEqualTo(0));
    }
}
