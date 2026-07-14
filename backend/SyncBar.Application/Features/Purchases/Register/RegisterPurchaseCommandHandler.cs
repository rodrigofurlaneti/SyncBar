using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Purchases.Register;

internal sealed class RegisterPurchaseCommandHandler(
    IPurchaseRepository purchaseRepository,
    IStockItemRepository stockItemRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterPurchaseCommand, long>
{
    public async Task<Result<long>> Handle(RegisterPurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchaseResult = Purchase.Create(
            request.BranchId, request.SupplierId, request.DocumentNumber, request.PurchasedAt, request.Notes);
        if (purchaseResult.IsFailure)
            return Result.Failure<long>(purchaseResult.Error);

        var purchase = purchaseResult.Value;
        foreach (var item in request.Items)
        {
            var added = purchase.AddItem(item.ProductId, item.Quantity, item.UnitCost);
            if (added.IsFailure)
                return Result.Failure<long>(added.Error);
        }

        // Salva a compra e os itens primeiro — precisamos dos Ids gerados (PurchaseItem.Id)
        // para referenciar no livro-razão de estoque (StockMovement.PurchaseItemId).
        await purchaseRepository.AddAsync(purchase, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        // Entrada de estoque: sobe o saldo e registra o movimento — nunca um sem o outro.
        foreach (var item in purchase.Items)
        {
            var stockItem = await stockItemRepository.GetByBranchAndProductForUpdateAsync(
                request.BranchId, item.ProductId, cancellationToken);
            if (stockItem is null)
            {
                var created = StockItem.Create(request.BranchId, item.ProductId, 0, null);
                if (created.IsFailure)
                    return Result.Failure<long>(created.Error);
                stockItem = created.Value;
                await stockItemRepository.AddAsync(stockItem, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken); // garante Id antes do movimento
            }

            var increased = stockItem.Increase(item.Quantity);
            if (increased.IsFailure)
                return Result.Failure<long>(increased.Error);

            var movement = StockMovement.Create(
                stockItem.Id, StockMovementTypeIds.EntradaCompra, item.Id, null, request.EmployeeId,
                item.Quantity, item.UnitCost, item.TotalCost, request.DocumentNumber, request.PurchasedAt,
                request.Notes);
            if (movement.IsFailure)
                return Result.Failure<long>(movement.Error);

            await stockMovementRepository.AddAsync(movement.Value, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(purchase.Id);
    }
}
