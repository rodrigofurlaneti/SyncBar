using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.AddPizzaItem;

internal sealed class AddPizzaOrderItemCommandHandler(
    ICustomerOrderRepository orderRepository,
    IProductRepository productRepository,
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    IProductStockRepository stockRepository,
    IPrintingService printingService,
    TimeProvider timeProviderCustom,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddPizzaOrderItemCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(AddPizzaOrderItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddPizzaOrderItemCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                userIdBox.Value = request.EmployeeId;

                var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));

                var configuration = await pizzaConfigurationRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
                if (configuration is null)
                    return Result.Failure(new Error("PizzaConfiguration.NotFound", "This product has no pizza configuration."));

                // Regra de negócio (decisão do SyncBar): sabor mais caro entre os escolhidos +
                // borda + recheio de borda — ver PizzaConfiguration.CalculateUnitPrice.
                var priceResult = configuration.CalculateUnitPrice(
                    request.PizzaSizeId, request.PizzaCrustId, request.PizzaEdgeId, request.PizzaFlavorIds);
                if (priceResult.IsFailure)
                    return Result.Failure(priceResult.Error);

                var stockSnapshot = await stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

                var itemCountBefore = order.Items.Count;
                var currentTime = timeProviderCustom.GetLocalNow().DateTime;

                var result = order.AddPizzaItem(
                    product.Id, priceResult.Value, request.Quantity, request.Notes, request.EmployeeId, currentTime,
                    request.PizzaSizeId, request.PizzaCrustId, request.PizzaEdgeId, request.PizzaFlavorIds);
                if (result.IsFailure)
                    return result;

                if (stockSnapshot is not null)
                {
                    var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

                    var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
                    if (stockResult.IsFailure) return Result.Failure(stockResult.Error);

                    long? movementEmployeeId = request.EmployeeId is > 0 ? request.EmployeeId : null;

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
                        notes: $"Baixa automática do pedido {order.Id} (pizza)"
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
                if (newItemIds.Count > 0)
                {
                    _ = printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
                }

                return Result.Success();
            });
    }
}
