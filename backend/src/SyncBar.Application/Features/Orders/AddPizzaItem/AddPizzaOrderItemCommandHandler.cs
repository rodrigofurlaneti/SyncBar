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
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task<Result> Handle(AddPizzaOrderItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddPizzaOrderItemCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                userIdBox.Value = request.EmployeeId;

                var flavorsValidation = ValidateFlavorsSelected(request);
                if (flavorsValidation.IsFailure)
                    return flavorsValidation;

                var orderResult = await LoadActiveOrderAsync(request.CustomerOrderId, cancellationToken);
                if (orderResult.IsFailure)
                    return Result.Failure(orderResult.Error);
                var order = orderResult.Value;

                var productResult = await LoadActiveProductAsync(request.ProductId, cancellationToken);
                if (productResult.IsFailure)
                    return Result.Failure(productResult.Error);
                var product = productResult.Value;

                var configurationResult = await LoadPizzaConfigurationAsync(request.ProductId, cancellationToken);
                if (configurationResult.IsFailure)
                    return Result.Failure(configurationResult.Error);

                var priceResult = CalculatePizzaUnitPrice(configurationResult.Value, request);
                if (priceResult.IsFailure)
                    return Result.Failure(priceResult.Error);

                var stockSnapshot = await stockRepository.GetByProductIdAsync(product.Id, cancellationToken);

                var itemCountBefore = order.Items.Count;
                var currentTime = timeProviderCustom.GetLocalNow().DateTime;

                var addItemResult = order.AddPizzaItem(
                    product.Id, priceResult.Value, request.Quantity, request.Notes, request.EmployeeId, currentTime,
                    request.PizzaSizeId, request.PizzaCrustId, request.PizzaEdgeId, request.PizzaFlavorIds);
                if (addItemResult.IsFailure)
                    return addItemResult;

                if (stockSnapshot is not null)
                {
                    var stockDeductionResult = DeductStockForPizza(
                        stockSnapshot, order, itemCountBefore, request.EmployeeId, currentTime);
                    if (stockDeductionResult.IsFailure)
                        return stockDeductionResult;
                }

                var commitResult = await CommitOrderAsync(cancellationToken);
                if (commitResult.IsFailure)
                    return commitResult;

                PrintNewItems(order, itemCountBefore);

                return Result.Success();
            });
    }

    private static Result ValidateFlavorsSelected(AddPizzaOrderItemCommand request)
    {
        if (request.PizzaFlavorIds is not { Count: > 0 })
            return Result.Failure(new Error("PizzaConfiguration.NoFlavorsSelected", "At least one flavor must be selected."));

        return Result.Success();
    }

    private async Task<Result<CustomerOrder>> LoadActiveOrderAsync(long customerOrderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(customerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<CustomerOrder>(new Error("CustomerOrder.NotFound", "Order not found."));

        return Result.Success(order);
    }

    private async Task<Result<Product>> LoadActiveProductAsync(long productId, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<Product>(new Error("Product.NotFound", "Product not found."));

        return Result.Success(product);
    }

    private async Task<Result<PizzaConfiguration>> LoadPizzaConfigurationAsync(long productId, CancellationToken cancellationToken)
    {
        var configuration = await pizzaConfigurationRepository.GetByProductIdAsync(productId, cancellationToken);
        if (configuration is null)
            return Result.Failure<PizzaConfiguration>(new Error("PizzaConfiguration.NotFound", "This product has no pizza configuration."));

        return Result.Success(configuration);
    }

    // Regra de negócio (decisão do SyncBar): sabor mais caro entre os escolhidos +
    // borda + recheio de borda — ver PizzaConfiguration.CalculateUnitPrice.
    private static Result<decimal> CalculatePizzaUnitPrice(PizzaConfiguration configuration, AddPizzaOrderItemCommand request)
        => configuration.CalculateUnitPrice(
            request.PizzaSizeId, request.PizzaCrustId, request.PizzaEdgeId, request.PizzaFlavorIds);

    private Result DeductStockForPizza(
        ProductStock stockSnapshot, CustomerOrder order, int itemCountBefore, long? employeeId, DateTime currentTime)
    {
        var totalQuantityAdded = order.Items.Skip(itemCountBefore).Sum(i => i.Quantity);

        var stockResult = stockSnapshot.Deduct(totalQuantityAdded);
        if (stockResult.IsFailure)
            return Result.Failure(stockResult.Error);

        long? movementEmployeeId = employeeId is > 0 ? employeeId : null;

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

        if (movementResult.IsFailure)
            return Result.Failure(movementResult.Error);

        stockRepository.AddMovement(movementResult.Value);
        return Result.Success();
    }

    private async Task<Result> CommitOrderAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(new Error("Stock.Concurrency",
                "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
        }
    }

    private void PrintNewItems(CustomerOrder order, int itemCountBefore)
    {
        var newItemIds = order.Items.Skip(itemCountBefore).Select(i => i.Id).ToList();
        if (newItemIds.Count > 0)
        {
            _ = printingService.PrintOrderItemsAsync(order.Id, newItemIds, CancellationToken.None);
        }
    }
}
