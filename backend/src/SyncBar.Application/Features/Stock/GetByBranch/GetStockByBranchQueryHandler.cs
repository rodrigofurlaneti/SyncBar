using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.GetByBranch;

internal sealed class GetStockByBranchQueryHandler(IStockItemRepository stockItemRepository)
    : IQueryHandler<GetStockByBranchQuery, IReadOnlyCollection<StockItemResponse>>
{
    public async Task<Result<IReadOnlyCollection<StockItemResponse>>> Handle(
        GetStockByBranchQuery request, CancellationToken cancellationToken)
    {
        var items = await stockItemRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        IReadOnlyCollection<StockItemResponse> response = items
            .OrderBy(i => i.ProductId)
            .Select(i => new StockItemResponse(
                i.Id, i.BranchId, i.ProductId, i.CurrentQuantity,
                i.MinimumQuantity, i.MaximumQuantity, i.IsBelowMinimum()))
            .ToList();

        return Result.Success(response);
    }
}
