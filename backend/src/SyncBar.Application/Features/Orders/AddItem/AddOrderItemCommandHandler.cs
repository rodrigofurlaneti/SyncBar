using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.AddItem;

internal sealed class AddOrderItemCommandHandler : BaseCommandHandler<AddOrderItemCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IProductStockRepository _stockRepository;
    private readonly IPrintingService _printingService;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemCommandHandler(
        ICustomerOrderRepository orderRepository,
        IProductRepository productRepository,
        IPromotionRepository promotionRepository,
        IProductStockRepository stockRepository,
        IPrintingService printingService,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _promotionRepository = promotionRepository;
        _stockRepository = stockRepository;
        _printingService = printingService;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddOrderItemCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Mapeia o ID do funcionário (usuário) responsável pela ação para o log de auditoria
                userIdBox.Value = request.EmployeeId;

                // 1. Busca o pedido sequencialmente
                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                // 2. Busca o produto sequencialmente
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));

                // 3. Busca as promoções sequencialmente
                var promotions = await _promotionRepository.GetByBranchAsync(order.BranchId, cancellationToken);

                // 4. Busca o estoque sequencialmente
                var stockSnapshot = await _stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

                var itemCountBefore = order.Items.Count;
                // O domínio usa DateTime puro (DATETIME2 no banco), não DateTimeOffset —
                // e o padrão do projeto é hora LOCAL (ver OpenOrderCommandHandler), não UTC,
                // já que o front-end interpreta as datas recebidas como hora local sem conversão.
                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var activePromotion = promotions.FirstOrDefault(promo =>
                    promo.ProductId == product.Id && promo.IsActiveAt(currentTime));

                var result = order.AddItemWithPromotion(product, request.Quantity, request.Notes, activePromotion, request.EmployeeId ?? 0, currentTime);
                if (result.IsFailure)
                    return result;

                if (stockSnapshot is not null)
                {
                    var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

                    var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
                    if (stockResult.IsFailure) return Result.Failure(stockResult.Error);

                    long? movementEmployeeId = request.EmployeeId != 0 ? request.EmployeeId : null;

                    var movementResult = StockMovement.Create(
                        stockItemId: stockSnapshot.ProductId,
                        stockMovementTypeId: 2, // Tipo: Venda/Saída
                        purchaseItemId: null,
                        orderItemId: order.Items.Last().Id,
                        employeeId: movementEmployeeId,
                        quantity: -totalQuantityAdded,
                        unitCost: null,
                        totalCost: null,
                        documentNumber: null,
                        movedAt: currentTime,
                        notes: $"Baixa automática do pedido {order.Id}"
                    );

                    if (movementResult.IsFailure) return Result.Failure(movementResult.Error);

                    _stockRepository.AddMovement(movementResult.Value);
                }

                try
                {
                    await _unitOfWork.CommitAsync(cancellationToken);
                }
                catch (ConcurrencyException)
                {
                    return Result.Failure(new Error("Stock.Concurrency",
                        "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
                }

                var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
                if (newItemIds.Any())
                {
                    _ = _printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
                }

                return Result.Success();
            });
    }
}