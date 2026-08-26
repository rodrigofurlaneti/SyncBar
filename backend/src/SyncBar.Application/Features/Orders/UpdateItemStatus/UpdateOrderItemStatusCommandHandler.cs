using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

internal sealed class UpdateOrderItemStatusCommandHandler : BaseCommandHandler<UpdateOrderItemStatusCommand>
{
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

                if (request.OrderItemStatusId == Domain.Constants.OrderItemStatusIds.Cancelado && !request.IsManager)
                {
                    var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                    if (item is not null && item.OrderItemStatusId != Domain.Constants.OrderItemStatusIds.Lancado)
                        return Result.Failure(new Error("OrderItem.CancelRequiresManager",
                            "Item já enviado à cozinha — somente o gerente pode cancelar."));
                }

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;
                var result = order.UpdateItemStatus(request.OrderItemId, request.OrderItemStatusId, currentTime, request.ActorEmployeeId);

                if (result.IsFailure)
                    return result;

                const long statusProntoId = 4;
                if (request.OrderItemStatusId == statusProntoId)
                {
                    string tableInfo = "Comanda/Balcão";
                    var targetDiningAreaIds = new HashSet<long>(); 
                    if (order.DiningTableId.HasValue)
                    {
                        var table = await _diningTableRepository.GetByIdAsync(order.DiningTableId.Value, cancellationToken);
                        if (table is not null)
                        {
                            tableInfo = $"Mesa {table.Number}";
                        }
                        var areaTable = await _diningAreaTableRepository.GetByTableIdAsync(order.DiningTableId.Value, cancellationToken);
                        if (areaTable is not null)
                        {
                            targetDiningAreaIds.Add(areaTable.DiningAreaId);
                        }
                        else
                        {
                            return Result.Failure(new Error("WaiterMessage.DiningAreaRequired", "Não foi possível identificar a praça da mesa para registrar a mensagem."));
                        }
                    }
                    else
                    {
                        if (order.ComandaId > 0)
                        {
                            tableInfo = $"Comanda {order.ComandaId}";
                        }

                        var allAreas = await _diningAreaRepository.GetByBranchIdAsync(order.BranchId, cancellationToken);
                        if (allAreas != null && allAreas.Any())
                        {
                            foreach (var area in allAreas)
                            {
                                targetDiningAreaIds.Add(area.Id);
                            }
                        }
                        else
                        {
                            targetDiningAreaIds.Add(1);
                        }
                    }
                    var targetItem = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                    string productName = "Item";
                    if (targetItem is not null)
                    {
                        var product = await _productRepository.GetByIdAsync(targetItem.ProductId, cancellationToken);
                        if (product is not null)
                        {
                            productName = product.Name;
                        }
                    }
                    string messageText = $"{productName} ({tableInfo}) do pedido #{order.Id} está PRONTO para servir.";
                    foreach (var areaId in targetDiningAreaIds)
                    {
                        var waiterMessageResult = WaiterMessage.Create(
                            branchId: order.BranchId,
                            senderEmployeeId: request.ActorEmployeeId ?? 1,
                            recipientEmployeeId: order.EmployeeId,
                            diningAreaId: areaId,
                            message: messageText
                        );

                        if (waiterMessageResult.IsSuccess)
                        {
                            await _messageRepository.AddAsync(waiterMessageResult.Value, cancellationToken);
                        }
                    }
                }
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}