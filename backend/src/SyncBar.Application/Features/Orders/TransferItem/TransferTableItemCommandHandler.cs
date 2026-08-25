using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.TransferItem;

internal sealed class TransferTableItemCommandHandler : BaseCommandHandler<TransferTableItemCommand, long>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ITableItemTransferRepository _transferRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public TransferTableItemCommandHandler(
        ICustomerOrderRepository orderRepository,
        ITableItemTransferRepository transferRepository,
        TimeProvider timeProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _transferRepository = transferRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(TransferTableItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(TransferTableItemCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                userIdBox.Value = request.ActorEmployeeId;
                var sourceOrder = await _orderRepository.GetByIdForUpdateAsync(request.SourceCustomerOrderId, cancellationToken);
                if (sourceOrder is null || !sourceOrder.IsActive)
                    return Result.Failure<long>(new Error("CustomerOrder.SourceNotFound", "Source order not found."));
                var itemToTransfer = sourceOrder.Items.FirstOrDefault(i => i.Id == request.CustomerOrderItemId);
                if (itemToTransfer is null)
                    return Result.Failure<long>(new Error("CustomerOrderItem.NotFound", "Item not found in source order."));
                if (itemToTransfer.OrderItemStatusId == OrderItemStatusIds.Cancelado)
                    return Result.Failure<long>(new Error("OrderItem.AlreadyCancelled", "Itens cancelados não podem ser transferidos."));
                var originalStatusId = itemToTransfer.OrderItemStatusId;
                var targetOrder = await _orderRepository.GetByIdForUpdateAsync(request.TargetCustomerOrderId, cancellationToken);
                if (targetOrder is null || !targetOrder.IsActive)
                    return Result.Failure<long>(new Error("CustomerOrder.TargetNotFound", "Target order not found."));
                var currentTime = _timeProvider.GetLocalNow().DateTime;
                var cancelResult = sourceOrder.ForceCancelItemForTransfer(
                    itemToTransfer.Id,
                    currentTime,
                    request.ActorEmployeeId
                );
                if (cancelResult.IsFailure)
                    return Result.Failure<long>(cancelResult.Error);
                var addResult = targetOrder.AddItem(
                    itemToTransfer.ProductId,
                    itemToTransfer.UnitPrice,
                    itemToTransfer.Quantity,
                    itemToTransfer.Notes,
                    request.ActorEmployeeId,
                    currentTime
                );
                if (addResult.IsFailure)
                    return Result.Failure<long>(addResult.Error);
                var newlyAddedItem = targetOrder.Items.Last();
                if (newlyAddedItem.OrderItemStatusId != originalStatusId)
                {
                    var statusResult = targetOrder.UpdateItemStatus(
                        newlyAddedItem.Id,
                        originalStatusId,
                        currentTime,
                        request.ActorEmployeeId
                    );
                    if (statusResult.IsFailure)
                        return Result.Failure<long>(statusResult.Error);
                }
                var transferResult = TableItemTransfer.Create(
                    request.SourceCustomerOrderId,
                    request.CustomerOrderItemId,
                    request.SourceDiningTableId,
                    request.TargetDiningTableId,
                    request.ActorEmployeeId
                );
                if (transferResult.IsFailure)
                    return Result.Failure<long>(transferResult.Error);
                await _transferRepository.AddAsync(transferResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(transferResult.Value.Id);
            });
    }
}