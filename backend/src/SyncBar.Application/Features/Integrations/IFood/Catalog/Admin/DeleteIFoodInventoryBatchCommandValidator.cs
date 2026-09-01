using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

public sealed class DeleteIfoodInventoryBatchCommandValidator : AbstractValidator<DeleteIfoodInventoryBatchCommand>
{
    public DeleteIfoodInventoryBatchCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductIds).NotEmpty();
    }
}
