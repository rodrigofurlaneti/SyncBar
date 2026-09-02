using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.TransferTableItems;

internal sealed class TransferTableItemsCommandHandler : BaseCommandHandler<TransferTableItemsCommand, Unit>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ITableItemTransferRepository _transferRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public TransferTableItemsCommandHandler(
        ICustomerOrderRepository orderRepository,
        ITableItemTransferRepository transferRepository,
        IDiningTableRepository diningTableRepository,
        TimeProvider timeProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _transferRepository = transferRepository;
        _diningTableRepository = diningTableRepository;
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

                // Ordena os itens selecionados de forma crescente para processar em ordem limpa
                var sortedItemIds = request.CustomerOrderItemIds.OrderBy(id => id).ToList();

                foreach (var itemId in sortedItemIds)
                {
                    var itemToTransfer = sourceOrder.Items.FirstOrDefault(i => i.Id == itemId);
                    if (itemToTransfer is null)
                        return Result.Failure<Unit>(new Error("CustomerOrderItem.NotFound", $"Item {itemId} not found in source order."));

                    if (itemToTransfer.OrderItemStatusId == OrderItemStatusIds.Cancelado)
                        return Result.Failure<Unit>(new Error("OrderItem.AlreadyCancelled", "Itens cancelados não podem ser transferidos."));

                    var originalStatusId = itemToTransfer.OrderItemStatusId;

                    // Cancela na origem para transferência
                    var cancelResult = sourceOrder.ForceCancelItemForTransfer(itemToTransfer.Id, currentTime, request.ActorEmployeeId);
                    if (cancelResult.IsFailure)
                        return Result.Failure<Unit>(cancelResult.Error);

                    // Adiciona no destino
                    var addResult = targetOrder.AddItem(
                        itemToTransfer.ProductId,
                        itemToTransfer.UnitPrice,
                        itemToTransfer.Quantity,
                        itemToTransfer.Notes,
                        request.ActorEmployeeId,
                        currentTime);

                    if (addResult.IsFailure)
                        return Result.Failure<Unit>(addResult.Error);

                    // Restaura o status original no destino
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

                // Verifica se ainda sobrou algum produto ativo na mesa de origem
                bool hasActiveItems = sourceOrder.Items.Any(i => i.OrderItemStatusId != OrderItemStatusIds.Cancelado);

                var sourceTable = await _diningTableRepository.GetByIdForUpdateAsync(request.SourceDiningTableId, cancellationToken);
                if (sourceTable is not null)
                {
                    if (!hasActiveItems)
                    {
                        // Se não sobrou nada, libera a mesa e cancela o pedido vazio
                        sourceTable.SetAvailable();
                        var cancelOrderResult = sourceOrder.Cancel(currentTime);
                        if (cancelOrderResult.IsFailure)
                            return Result.Failure<Unit>(cancelOrderResult.Error);
                    }
                    else
                    {
                        // Se ainda há produtos na mesa, garante que ela continue ocupada
                        sourceTable.SetInUse();
                    }
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(Unit.Value);
            });
    }
}