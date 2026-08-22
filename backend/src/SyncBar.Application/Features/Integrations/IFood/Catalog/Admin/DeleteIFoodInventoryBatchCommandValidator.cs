using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

public sealed class DeleteIFoodInventoryBatchCommandValidator : AbstractValidator<DeleteIFoodInventoryBatchCommand>
{
    public DeleteIFoodInventoryBatchCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductIds).NotEmpty();
    }
}
