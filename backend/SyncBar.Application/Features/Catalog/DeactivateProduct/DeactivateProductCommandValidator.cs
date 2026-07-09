using FluentValidation;

namespace SyncBar.Application.Features.Catalog.DeactivateProduct;

public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
