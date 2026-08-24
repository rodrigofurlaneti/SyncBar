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
                    var adjustmentResult = await ProcessCountAsync(
                        request.BranchId, request.EmployeeId, count.ProductId, count.CountedQuantity, cancellationToken);
                    if (adjustmentResult.IsFailure)
                        return Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(adjustmentResult.Error);

                    if (adjustmentResult.Value is not null)
                        adjustments.Add(adjustmentResult.Value);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success<IReadOnlyCollection<InventoryAdjustmentResponse>>(adjustments);
            });
    }

    // Processa uma linha de contagem: garante o StockItem, calcula a diferença e,
    // quando houver divergência, aplica o ajuste de saldo e gera o StockMovement correspondente.
    // Retorna null (sucesso sem ajuste) quando a contagem bate com o saldo atual.
    private async Task<Result<InventoryAdjustmentResponse?>> ProcessCountAsync(
        long branchId, long employeeId, long productId, decimal countedQuantity, CancellationToken cancellationToken)
    {
        var stockItemResult = await GetOrCreateStockItemAsync(branchId, productId, cancellationToken);
        if (stockItemResult.IsFailure)
            return Result.Failure<InventoryAdjustmentResponse?>(stockItemResult.Error);

        var stockItem = stockItemResult.Value;
        var previous = stockItem.CurrentQuantity;
        var difference = countedQuantity - previous;
        if (difference == 0)
            return Result.Success<InventoryAdjustmentResponse?>(null); // contagem bateu — nada a ajustar

        var balanceResult = ApplyBalanceAdjustment(stockItem, difference);
        if (balanceResult.IsFailure)
            return Result.Failure<InventoryAdjustmentResponse?>(balanceResult.Error);

        var movementResult = CreateAdjustmentMovement(stockItem.Id, employeeId, difference);
        if (movementResult.IsFailure)
            return Result.Failure<InventoryAdjustmentResponse?>(movementResult.Error);

        await _stockMovementRepository.AddAsync(movementResult.Value, cancellationToken);

        return Result.Success<InventoryAdjustmentResponse?>(
            new InventoryAdjustmentResponse(productId, previous, countedQuantity, difference));
    }

    // Item ainda sem saldo entra no inventário com saldo zero.
    private async Task<Result<StockItem>> GetOrCreateStockItemAsync(
        long branchId, long productId, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(
            branchId, productId, cancellationToken);
        if (stockItem is not null)
            return Result.Success(stockItem);

        var created = StockItem.Create(branchId, productId, 0, null);
        if (created.IsFailure)
            return Result.Failure<StockItem>(created.Error);

        stockItem = created.Value;
        await _stockItemRepository.AddAsync(stockItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(stockItem);
    }

    // Toda correção passa pelo livro-razão: AjusteEntrada (sobra) / AjusteSaida (falta).
    private static Result ApplyBalanceAdjustment(StockItem stockItem, decimal difference)
        => difference > 0
            ? stockItem.Increase(difference)
            : stockItem.Decrease(-difference);

    private static Result<StockMovement> CreateAdjustmentMovement(long stockItemId, long employeeId, decimal difference)
        => StockMovement.Create(
            stockItemId,
            difference > 0 ? StockMovementTypeIds.AjusteEntrada : StockMovementTypeIds.AjusteSaida,
            null,
            null,
            employeeId,
            Math.Abs(difference),
            null,
            null,
            null,
            DateTime.Now, "Inventário");
}