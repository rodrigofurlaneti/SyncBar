using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.AdjustInventory;

internal sealed class AdjustInventoryCommandHandler : BaseCommandHandler<AdjustInventoryCommand, IReadOnlyCollection<InventoryAdjustmentResponse>>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustInventoryCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<IReadOnlyCollection<InventoryAdjustmentResponse>>> Handle(
        AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AdjustInventoryCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Associa a ação no log de auditoria ao funcionário responsável pelo inventário
                userIdBox.Value = request.EmployeeId;

                var adjustments = new List<InventoryAdjustmentResponse>();

                foreach (var count in request.Counts)
                {
                    // Item ainda sem saldo entra no inventario com saldo zero.
                    var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(
                        request.BranchId, count.ProductId, cancellationToken);
                    if (stockItem is null)
                    {
                        var created = StockItem.Create(request.BranchId, count.ProductId, 0, null);
                        if (created.IsFailure)
                            return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(created.Error);

                        stockItem = created.Value;
                        await _stockItemRepository.AddAsync(stockItem, cancellationToken);
                        await _unitOfWork.CommitAsync(cancellationToken);
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
                        null,
                        null,
                        request.EmployeeId,
                        Math.Abs(difference),
                        null,
                        null,
                        null,
                        DateTime.Now, "Inventário");

                    if (movement.IsFailure)
                        return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(movement.Error);

                    await _stockMovementRepository.AddAsync(movement.Value, cancellationToken);
                    adjustments.Add(new InventoryAdjustmentResponse(
                        count.ProductId, previous, count.CountedQuantity, difference));
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success<IReadOnlyCollection<InventoryAdjustmentResponse>>(adjustments);
            });
    }
}