using FluentValidation;

namespace SyncBar.Application.Features.Stock.RegisterMovement;

public sealed class RegisterStockMovementCommandValidator : AbstractValidator<RegisterStockMovementCommand>
{
    public RegisterStockMovementCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.StockMovementTypeId).InclusiveBetween(1, 10);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
        RuleFor(x => x.DocumentNumber).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}
