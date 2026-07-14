using FluentValidation;

namespace SyncBar.Application.Features.Purchases.Register;

public sealed class RegisterPurchaseCommandValidator : AbstractValidator<RegisterPurchaseCommand>
{
    public RegisterPurchaseCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.DocumentNumber).MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Purchase requires at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
