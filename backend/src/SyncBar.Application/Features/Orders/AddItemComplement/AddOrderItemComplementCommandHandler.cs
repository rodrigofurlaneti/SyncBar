using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.AddItemComplement;

internal sealed class AddOrderItemComplementCommandHandler : BaseCommandHandler<AddOrderItemComplementCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IProductStockRepository _stockRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemComplementCommandHandler(
        ICustomerOrderRepository orderRepository,
        IComplementGroupRepository complementGroupRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        IComplementItemRepository complementItemRepository,
        IProductStockRepository stockRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _complementGroupRepository = complementGroupRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _complementItemRepository = complementItemRepository;
        _stockRepository = stockRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(AddOrderItemComplementCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddOrderItemComplementCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                userIdBox.Value = request.EmployeeId;

                var orderResult = await LoadActiveOrderAsync(request.CustomerOrderId, cancellationToken);
                if (orderResult.IsFailure)
                    return Result.Failure(orderResult.Error);
                var order = orderResult.Value;

                var itemResult = GetActiveOrderItem(order, request.OrderItemId);
                if (itemResult.IsFailure)
                    return Result.Failure(itemResult.Error);
                var item = itemResult.Value;

                var availabilityResult = await EnsureComplementGroupAvailableAsync(
                    item.ProductId, request.ComplementGroupId, cancellationToken);
                if (availabilityResult.IsFailure)
                    return availabilityResult;

                var groupResult = await LoadActiveComplementGroupAsync(request.ComplementGroupId, cancellationToken);
                if (groupResult.IsFailure)
                    return Result.Failure(groupResult.Error);

                var complementResult = GetActiveComplement(groupResult.Value, request.ComplementId);
                if (complementResult.IsFailure)
                    return Result.Failure(complementResult.Error);
                var complement = complementResult.Value;

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var addComplementResult = order.AddComplement(request.OrderItemId, complement.Id, complement.ExtraPrice, currentTime);
                if (addComplementResult.IsFailure)
                    return addComplementResult;

                var linkedStockResult = await DeductLinkedProductStockAsync(
                    complement, order, item, request.EmployeeId, currentTime, cancellationToken);
                if (linkedStockResult.IsFailure)
                    return linkedStockResult;

                return await CommitAsync(cancellationToken);
            });
    }

    private async Task<Result<CustomerOrder>> LoadActiveOrderAsync(long customerOrderId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(customerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<CustomerOrder>(new Error("CustomerOrder.NotFound", "Order not found."));

        return Result.Success(order);
    }

    private static Result<OrderItem> GetActiveOrderItem(CustomerOrder order, long orderItemId)
    {
        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId && i.IsActive);
        if (item is null)
            return Result.Failure<OrderItem>(new Error("CustomerOrder.ItemNotFound", "Order item not found."));

        return Result.Success(item);
    }

    private async Task<Result> EnsureComplementGroupAvailableAsync(
        long productId, long complementGroupId, CancellationToken cancellationToken)
    {
        var links = await _productComplementGroupRepository.GetByProductAsync(productId, cancellationToken);
        if (links.All(l => l.ComplementGroupId != complementGroupId))
            return Result.Failure(new Error("OrderItem.ComplementGroupNotAvailable",
                "This complement group is not available for the item's product."));

        return Result.Success();
    }

    private async Task<Result<ComplementGroup>> LoadActiveComplementGroupAsync(
        long complementGroupId, CancellationToken cancellationToken)
    {
        var group = await _complementGroupRepository.GetByIdAsync(complementGroupId, cancellationToken);
        if (group is null || !group.IsActive)
            return Result.Failure<ComplementGroup>(new Error("ComplementGroup.NotFound", "Complement group not found."));

        return Result.Success(group);
    }

    private static Result<Complement> GetActiveComplement(ComplementGroup group, long complementId)
    {
        var complement = group.Complements.FirstOrDefault(c => c.Id == complementId && c.IsActive);
        if (complement is null)
            return Result.Failure<Complement>(new Error("ComplementGroup.ComplementNotFound", "Complement not found in this group."));

        return Result.Success(complement);
    }

    // Fase 18 (combos) — mesmo critério de AddOrderItemCommandHandler: se este
    // complemento aponta pra um Product real (LinkedProductId), baixa o estoque
    // daquele produto também, na quantidade da linha do pedido a que o complemento foi
    // adicionado (o item já existe — pega a Quantity dele, não é sempre 1).
    private async Task<Result> DeductLinkedProductStockAsync(
        Complement complement,
        CustomerOrder order,
        OrderItem item,
        long employeeId,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        var complementItem = await _complementItemRepository.GetByIdAsync(complement.ComplementItemId, cancellationToken);
        if (complementItem?.LinkedProductId is not { } linkedProductId)
            return Result.Success();

        var linkedStock = await _stockRepository.GetByProductIdAsync(linkedProductId, cancellationToken);
        if (linkedStock is null)
            return Result.Success();

        var linkedStockResult = linkedStock.Deduct(item.Quantity);
        if (linkedStockResult.IsFailure)
            return Result.Failure(linkedStockResult.Error);

        var linkedMovementEmployeeId = employeeId is > 0 ? employeeId : null;
        var linkedMovementResult = StockMovement.Create(
            stockItemId: linkedStock.ProductId,
            stockMovementTypeId: 2, // Tipo: Venda/Saída
            purchaseItemId: null,
            orderItemId: item.Id,
            employeeId: linkedMovementEmployeeId,
            quantity: -item.Quantity,
            unitCost: null,
            totalCost: null,
            documentNumber: null,
            movedAt: currentTime,
            notes: $"Baixa automática do pedido {order.Id} (combo — complemento {complement.Id})"
        );

        if (linkedMovementResult.IsFailure)
            return Result.Failure(linkedMovementResult.Error);

        _stockRepository.AddMovement(linkedMovementResult.Value);
        return Result.Success();
    }

    private async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(new Error("Stock.Concurrency",
                "O estoque deste produto foi alterado por outro pedido neste momento. Por favor, tente novamente."));
        }

        return Result.Success();
    }
}
