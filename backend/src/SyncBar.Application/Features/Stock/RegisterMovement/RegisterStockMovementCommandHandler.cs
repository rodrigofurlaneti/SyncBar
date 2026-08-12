using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.RegisterMovement;

internal sealed class RegisterStockMovementCommandHandler : BaseCommandHandler<RegisterStockMovementCommand, long>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly HashSet<long> InflowTypes =
    [
        StockMovementTypeIds.EntradaCompra,
        StockMovementTypeIds.AjusteEntrada,
        StockMovementTypeIds.TransferenciaEntrada
    ];

    public RegisterStockMovementCommandHandler(
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(RegisterStockMovementCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RegisterStockMovementCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Associa a ação no log de auditoria ao funcionário que está registrando a movimentação
                userIdBox.Value = request.EmployeeId;

                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

                // Saldo por filial x produto — cria o StockItem na primeira movimentacao.
                var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(
                    request.BranchId, request.ProductId, cancellationToken);
                if (stockItem is null)
                {
                    var created = StockItem.Create(request.BranchId, request.ProductId, 0, null);
                    if (created.IsFailure)
                        return Result.Failure<long>(created.Error);

                    stockItem = created.Value;
                    await _stockItemRepository.AddAsync(stockItem, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                }

                // Todo ajuste de saldo passa pelo livro-razao — nunca UPDATE direto.
                var isInflow = InflowTypes.Contains(request.StockMovementTypeId);
                var balance = isInflow ? stockItem.Increase(request.Quantity) : stockItem.Decrease(request.Quantity);
                if (balance.IsFailure)
                    return Result.Failure<long>(balance.Error);

                var movement = StockMovement.Create(
                    stockItem.Id,
                    request.StockMovementTypeId,
                    null,
                    null,
                    request.EmployeeId,
                    request.Quantity, request.UnitCost,
                    request.UnitCost is null ? null : Math.Round(request.UnitCost.Value * request.Quantity, 2),
                    request.DocumentNumber, DateTime.Now, request.Notes);

                if (movement.IsFailure)
                    return Result.Failure<long>(movement.Error);

                await _stockMovementRepository.AddAsync(movement.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(movement.Value.Id);
            });
    }
}