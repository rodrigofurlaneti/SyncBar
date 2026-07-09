using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.GetLedger;

internal sealed class GetStockLedgerQueryHandler(IStockMovementRepository stockMovementRepository)
    : IQueryHandler<GetStockLedgerQuery, IReadOnlyCollection<StockMovementResponse>>
{
    public async Task<Result<IReadOnlyCollection<StockMovementResponse>>> Handle(
        GetStockLedgerQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetByStockItemAsync(request.StockItemId, cancellationToken);

        // Ordenacao em C# — extrato do mais recente para o mais antigo.
        IReadOnlyCollection<StockMovementResponse> response = movements
            .OrderByDescending(m => m.MovedAt)
            .Select(m => new StockMovementResponse(
                m.Id, m.StockItemId, m.StockMovementTypeId, m.Quantity,
                m.UnitCost, m.TotalCost, m.DocumentNumber, m.MovedAt, m.Notes))
            .ToList();

        return Result.Success(response);
    }
}
