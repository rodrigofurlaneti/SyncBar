using FluentValidation;

namespace SyncBar.Application.Features.Catalog.ActivateProduct;

public sealed class ActivateProductCommandValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
