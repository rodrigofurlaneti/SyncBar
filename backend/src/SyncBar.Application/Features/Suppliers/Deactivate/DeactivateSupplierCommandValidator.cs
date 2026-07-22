using FluentValidation;

namespace SyncBar.Application.Features.Suppliers.Deactivate;

public sealed class DeactivateSupplierCommandValidator : AbstractValidator<DeactivateSupplierCommand>
{
    public DeactivateSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
    }
}
