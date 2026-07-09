using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.SetLimits;

internal sealed class SetStockLimitsCommandHandler(
    IStockItemRepository stockItemRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetStockLimitsCommand>
{
    public async Task<Result> Handle(SetStockLimitsCommand request, CancellationToken cancellationToken)
    {
        var stockItem = await stockItemRepository.GetByIdForUpdateAsync(request.StockItemId, cancellationToken);
        if (stockItem is null || !stockItem.IsActive)
            return Result.Failure(new Error("StockItem.NotFound", "Stock item not found."));

        var result = stockItem.SetLimits(request.MinimumQuantity, request.MaximumQuantity);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
