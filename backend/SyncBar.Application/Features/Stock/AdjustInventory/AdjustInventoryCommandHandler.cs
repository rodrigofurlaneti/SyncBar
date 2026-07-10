using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.AdjustInventory;

internal sealed class AdjustInventoryCommandHandler(
    IStockItemRepository stockItemRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdjustInventoryCommand, IReadOnlyCollection<InventoryAdjustmentResponse>>
{
    public async Task<Result<IReadOnlyCollection<InventoryAdjustmentResponse>>> Handle(
        AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var adjustments = new List<InventoryAdjustmentResponse>();

        foreach (var count in request.Counts)
        {
            // Item ainda sem saldo entra no inventario com saldo zero.
            var stockItem = await stockItemRepository.GetByBranchAndProductForUpdateAsync(
                request.BranchId, count.ProductId, cancellationToken);
            if (stockItem is null)
            {
                var created = StockItem.Create(request.BranchId, count.ProductId, 0, null);
                if (created.IsFailure)
                    return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(created.Error);
                stockItem = created.Value;
                await stockItemRepository.AddAsync(stockItem, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
            }

            var previous = stockItem.CurrentQuantity;
            var difference = count.CountedQuantity - previous;
            if (difference == 0)
                continue; // contagem bateu — nada a ajustar

            // Toda correcao passa pelo livro-razao: AjusteEntrada (sobra) / AjusteSaida (falta).
            var balance = difference > 0
                ? stockItem.Increase(difference)
                : stockItem.Decrease(-difference);
            if (balance.IsFailure)
                return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(balance.Error);

            var movement = StockMovement.Create(
                stockItem.Id,
                difference > 0 ? StockMovementTypeIds.AjusteEntrada : StockMovementTypeIds.AjusteSaida,
                null, null, request.EmployeeId,
                Math.Abs(difference), null, null, null,
                DateTime.UtcNow, "Inventário");
            if (movement.IsFailure)
                return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(movement.Error);

            await stockMovementRepository.AddAsync(movement.Value, cancellationToken);
            adjustments.Add(new InventoryAdjustmentResponse(
                count.ProductId, previous, count.CountedQuantity, difference));
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<InventoryAdjustmentResponse>>(adjustments);
    }
}
