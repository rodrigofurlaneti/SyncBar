using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

public sealed class DeleteIfoodItemCommandValidator : AbstractValidator<DeleteIfoodItemCommand>
{
    public DeleteIfoodItemCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
