using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

public sealed class DeleteIfoodProductCommandValidator : AbstractValidator<DeleteIfoodProductCommand>
{
    public DeleteIfoodProductCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
