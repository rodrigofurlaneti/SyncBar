using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.RegisterMovement;

internal sealed class RegisterStockMovementCommandHandler(
    IStockItemRepository stockItemRepository,
    IStockMovementRepository stockMovementRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterStockMovementCommand, long>
{
    private static readonly HashSet<long> InflowTypes =
    [
        StockMovementTypeIds.EntradaCompra,
        StockMovementTypeIds.AjusteEntrada,
        StockMovementTypeIds.TransferenciaEntrada
    ];

    public async Task<Result<long>> Handle(RegisterStockMovementCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

        // Saldo por filial x produto — cria o StockItem na primeira movimentacao.
        var stockItem = await stockItemRepository.GetByBranchAndProductForUpdateAsync(
            request.BranchId, request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            var created = StockItem.Create(request.BranchId, request.ProductId, 0, null);
            if (created.IsFailure)
                return Result.Failure<long>(created.Error);
            stockItem = created.Value;
            await stockItemRepository.AddAsync(stockItem, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        // Todo ajuste de saldo passa pelo livro-razao — nunca UPDATE direto.
        var isInflow = InflowTypes.Contains(request.StockMovementTypeId);
        var balance = isInflow ? stockItem.Increase(request.Quantity) : stockItem.Decrease(request.Quantity);
        if (balance.IsFailure)
            return Result.Failure<long>(balance.Error);

        var movement = StockMovement.Create(
            stockItem.Id, request.StockMovementTypeId, null, null, request.EmployeeId,
            request.Quantity, request.UnitCost,
            request.UnitCost is null ? null : Math.Round(request.UnitCost.Value * request.Quantity, 2),
            request.DocumentNumber, DateTime.UtcNow, request.Notes);
        if (movement.IsFailure)
            return Result.Failure<long>(movement.Error);

        await stockMovementRepository.AddAsync(movement.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(movement.Value.Id);
    }
}
