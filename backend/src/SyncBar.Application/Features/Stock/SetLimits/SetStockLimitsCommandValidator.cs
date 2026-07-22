using FluentValidation;

namespace SyncBar.Application.Features.Stock.SetLimits;

public sealed class SetStockLimitsCommandValidator : AbstractValidator<SetStockLimitsCommand>
{
    public SetStockLimitsCommandValidator()
    {
        RuleFor(x => x.StockItemId).GreaterThan(0);
        RuleFor(x => x.MinimumQuantity).GreaterThanOrEqualTo(0);
    }
}
