using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

internal sealed class UpdateOrderItemStatusCommandHandler : BaseCommandHandler<UpdateOrderItemStatusCommand>
{
    private const long StatusProntoId = 4;

    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IWaiterMessageRepository _messageRepository;
    private readonly IDiningAreaTableRepository _diningAreaTableRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDiningAreaRepository _diningAreaRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderItemStatusCommandHandler(
        ICustomerOrderRepository orderRepository,
        IWaiterMessageRepository messageRepository,
        IDiningAreaTableRepository diningAreaTableRepository,
        IDiningTableRepository diningTableRepository,
        IProductRepository productRepository,
        IDiningAreaRepository diningAreaRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _messageRepository = messageRepository;
        _diningAreaTableRepository = diningAreaTableRepository;
        _diningTableRepository = diningTableRepository;
        _productRepository = productRepository;
        _diningAreaRepository = diningAreaRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateOrderItemStatusCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                userIdBox.Value = request.ActorEmployeeId;
                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);

                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var guardResult = ValidateCancelRequiresManager(order, request);
                if (guardResult.IsFailure)
                    return guardResult;

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;
                var updateResult = order.UpdateItemStatus(request.OrderItemId, request.OrderItemStatusId, currentTime, request.ActorEmployeeId);
                if (updateResult.IsFailure)
                    return updateResult;

                if (request.OrderItemStatusId == StatusProntoId)
                {
                    var notifyResult = await NotifyItemReadyAsync(order, request, cancellationToken);
                    if (notifyResult.IsFailure)
                        return notifyResult;
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    private static Result ValidateCancelRequiresManager(CustomerOrder order, UpdateOrderItemStatusCommand request)
    {
        if (request.OrderItemStatusId != Domain.Constants.OrderItemStatusIds.Cancelado || request.IsManager)
            return Result.Success();

        var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
        if (item is not null && item.OrderItemStatusId != Domain.Constants.OrderItemStatusIds.Lancado)
            return Result.Failure(new Error("OrderItem.CancelRequiresManager",
                "Item já enviado à cozinha — somente o gerente pode cancelar."));

        return Result.Success();
    }

    private async Task<Result> NotifyItemReadyAsync(CustomerOrder order, UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        var targetResult = await ResolveNotificationTargetAsync(order, cancellationToken);
        if (targetResult.IsFailure)
            return Result.Failure(targetResult.Error);

        var (tableInfo, targetDiningAreaIds) = targetResult.Value;
        var productName = await ResolveProductNameAsync(order, request.OrderItemId, cancellationToken);
        var messageText = $"{productName} ({tableInfo}) do pedido #{order.Id} está PRONTO para servir.";

        foreach (var areaId in targetDiningAreaIds)
        {
            var waiterMessageResult = WaiterMessage.Create(
                branchId: order.BranchId,
                senderEmployeeId: request.ActorEmployeeId ?? 1,
                recipientEmployeeId: order.EmployeeId,
                diningAreaId: areaId,
                message: messageText);

            if (waiterMessageResult.IsSuccess)
                await _messageRepository.AddAsync(waiterMessageResult.Value, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result<(string TableInfo, HashSet<long> DiningAreaIds)>> ResolveNotificationTargetAsync(CustomerOrder order, CancellationToken cancellationToken)
    {
        var targetDiningAreaIds = new HashSet<long>();

        if (!order.DiningTableId.HasValue)
            return Result.Success(await ResolveComandaTargetAsync(order, targetDiningAreaIds, cancellationToken));

        var tableInfo = "Comanda/Balcão";
        var table = await _diningTableRepository.GetByIdAsync(order.DiningTableId.Value, cancellationToken);
        if (table is not null)
            tableInfo = $"Mesa {table.Number}";

        var areaTable = await _diningAreaTableRepository.GetByTableIdAsync(order.DiningTableId.Value, cancellationToken);
        if (areaTable is null)
            return Result.Failure<(string, HashSet<long>)>(new Error("WaiterMessage.DiningAreaRequired", "Não foi possível identificar a praça da mesa para registrar a mensagem."));

        targetDiningAreaIds.Add(areaTable.DiningAreaId);
        return Result.Success((tableInfo, targetDiningAreaIds));
    }

    private async Task<(string TableInfo, HashSet<long> DiningAreaIds)> ResolveComandaTargetAsync(CustomerOrder order, HashSet<long> targetDiningAreaIds, CancellationToken cancellationToken)
    {
        var tableInfo = order.ComandaId > 0 ? $"Comanda {order.ComandaId}" : "Comanda/Balcão";

        var allAreas = await _diningAreaRepository.GetByBranchIdAsync(order.BranchId, cancellationToken);
        if (allAreas != null && allAreas.Any())
        {
            foreach (var area in allAreas)
                targetDiningAreaIds.Add(area.Id);
        }
        else
        {
            targetDiningAreaIds.Add(1);
        }

        return (tableInfo, targetDiningAreaIds);
    }

    private async Task<string> ResolveProductNameAsync(CustomerOrder order, long orderItemId, CancellationToken cancellationToken)
    {
        var targetItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (targetItem is null)
            return "Item";

        var product = await _productRepository.GetByIdAsync(targetItem.ProductId, cancellationToken);
        return product?.Name ?? "Item";
    }
}
