using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.AddItem;

internal sealed class AddOrderItemCommandHandler(
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    IPromotionRepository promotionRepository,
    IProductStockRepository stockRepository,
    IPrintingService printingService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AddOrderItemCommand>
{
    public async Task<Result> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var orderTask = orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        var productTask = productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        await Task.WhenAll(orderTask, productTask);

        var order = orderTask.Result;
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var product = productTask.Result;
        if (product is null || !product.IsActive)
            return Result.Failure(new Error("Product.NotFound", "Product not found."));

        var promotionsTask = promotionRepository.GetByBranchAsync(order.BranchId, cancellationToken);
        var stockSnapshotTask = stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

        await Task.WhenAll(promotionsTask, stockSnapshotTask);

        var itemCountBefore = order.Items.Count;
        var currentTime = timeProvider.GetLocalNow().DateTime;

        var promotions = promotionsTask.Result;
        var activePromotion = promotions.FirstOrDefault(promo =>
            promo.ProductId == product.Id && promo.IsActiveAt(currentTime));

        // Corrigido aqui com ?? 0 para atender ao long esperado pelo método de domínio
        var result = order.AddItemWithPromotion(product, request.Quantity, request.Notes, activePromotion, request.EmployeeId ?? 0, currentTime);
        if (result.IsFailure)
            return result;

        var stockSnapshot = stockSnapshotTask.Result;
        if (stockSnapshot is not null)
        {
            var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

            var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
            if (stockResult.IsFailure) return Result.Failure(stockResult.Error);

            long? movementEmployeeId = request.EmployeeId != 0 ? request.EmployeeId : null;

            var movementResult = StockMovement.Create(
                stockItemId: stockSnapshot.StockItemId,
                stockMovementTypeId: 2,
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

            stockRepository.AddMovement(movementResult.Value);
        }

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(new Error("Stock.Concurrency",
                "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
        }

        var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
        if (newItemIds.Any())
        {
            _ = printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
        }

        return Result.Success();
    }
}