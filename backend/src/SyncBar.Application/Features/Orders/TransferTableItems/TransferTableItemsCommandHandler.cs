using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.TransferTableItems
{
    internal sealed class TransferTableItemsCommandHandler : BaseCommandHandler<TransferTableItemsCommand, Unit>
    {
        private readonly ICustomerOrderRepository _orderRepository;
        private readonly ITableItemTransferRepository _transferRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;
        public TransferTableItemsCommandHandler(
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
        public override async Task<Result<Unit>> Handle(TransferTableItemsCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(TransferTableItemsCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    userIdBox.Value = request.ActorEmployeeId;
                    var sourceOrder = await _orderRepository.GetByIdForUpdateAsync(request.SourceCustomerOrderId, cancellationToken);
                    if (sourceOrder is null || !sourceOrder.IsActive)
                        return Result.Failure<Unit>(new Error("CustomerOrder.SourceNotFound", "Source order not found."));
                    var targetOrder = await _orderRepository.GetByIdForUpdateAsync(request.TargetCustomerOrderId, cancellationToken);
                    if (targetOrder is null || !targetOrder.IsActive)
                        return Result.Failure<Unit>(new Error("CustomerOrder.TargetNotFound", "Target order not found."));
                    var currentTime = _timeProvider.GetLocalNow().DateTime;
                    foreach (var itemId in request.CustomerOrderItemIds)
                    {
                        var itemToTransfer = sourceOrder.Items.FirstOrDefault(i => i.Id == itemId);
                        if (itemToTransfer is null)
                            return Result.Failure<Unit>(new Error("CustomerOrderItem.NotFound", $"Item {itemId} not found in source order."));
                        if (itemToTransfer.OrderItemStatusId == OrderItemStatusIds.Cancelado)
                            return Result.Failure<Unit>(new Error("OrderItem.AlreadyCancelled", "Itens cancelados não podem ser transferidos."));
                        var originalStatusId = itemToTransfer.OrderItemStatusId;
                        var cancelResult = sourceOrder.ForceCancelItemForTransfer(itemToTransfer.Id, currentTime, request.ActorEmployeeId);
                        if (cancelResult.IsFailure)
                            return Result.Failure<Unit>(cancelResult.Error);
                        var addResult = targetOrder.AddItem(
                            itemToTransfer.ProductId,
                            itemToTransfer.UnitPrice,
                            itemToTransfer.Quantity,
                            itemToTransfer.Notes,
                            request.ActorEmployeeId,
                            currentTime);
                        if (addResult.IsFailure)
                            return Result.Failure<Unit>(addResult.Error);
                        var newlyAddedItem = targetOrder.Items.Last();
                        if (newlyAddedItem.OrderItemStatusId != originalStatusId)
                        {
                            var statusResult = targetOrder.UpdateItemStatus(newlyAddedItem.Id, originalStatusId, currentTime, request.ActorEmployeeId);
                            if (statusResult.IsFailure)
                                return Result.Failure<Unit>(statusResult.Error);
                        }
                        var transferResult = TableItemTransfer.Create(
                            request.SourceCustomerOrderId,
                            itemId,
                            request.SourceDiningTableId,
                            request.TargetDiningTableId,
                            request.ActorEmployeeId);
                        if (transferResult.IsFailure)
                            return Result.Failure<Unit>(transferResult.Error);
                        await _transferRepository.AddAsync(transferResult.Value, cancellationToken);
                    }
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(Unit.Value);
                });
        }
    }
}