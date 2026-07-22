using FluentValidation;

namespace SyncBar.Application.Features.Stock.AdjustInventory;

public sealed class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Counts).NotEmpty().WithMessage("Inventário requer ao menos uma contagem.");
        RuleForEach(x => x.Counts).ChildRules(count =>
        {
            count.RuleFor(c => c.ProductId).GreaterThan(0);
            count.RuleFor(c => c.CountedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}
