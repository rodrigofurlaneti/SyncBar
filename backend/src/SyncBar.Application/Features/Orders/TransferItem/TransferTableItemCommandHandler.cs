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
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public TransferTableItemCommandHandler(
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

    public override async Task<Result<long>> Handle(TransferTableItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(TransferTableItemCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                userIdBox.Value = request.ActorEmployeeId;

                var contextResult = await LoadTransferContextAsync(request, cancellationToken);
                if (contextResult.IsFailure)
                    return Result.Failure<long>(contextResult.Error);

                return await ApplyTransferAsync(request, contextResult.Value, cancellationToken);
            });
    }

    private async Task<Result<(CustomerOrder SourceOrder, OrderItem ItemToTransfer, CustomerOrder TargetOrder, long OriginalStatusId)>> LoadTransferContextAsync(
        TransferTableItemCommand request, CancellationToken cancellationToken)
    {
        var sourceOrder = await _orderRepository.GetByIdForUpdateAsync(request.SourceCustomerOrderId, cancellationToken);
        if (sourceOrder is null || !sourceOrder.IsActive)
            return Result.Failure<(CustomerOrder, OrderItem, CustomerOrder, long)>(new Error("CustomerOrder.SourceNotFound", "Source order not found."));

        var itemToTransfer = sourceOrder.Items.FirstOrDefault(i => i.Id == request.CustomerOrderItemId);
        if (itemToTransfer is null)
            return Result.Failure<(CustomerOrder, OrderItem, CustomerOrder, long)>(new Error("CustomerOrderItem.NotFound", "Item not found in source order."));

        if (itemToTransfer.OrderItemStatusId == OrderItemStatusIds.Cancelado)
            return Result.Failure<(CustomerOrder, OrderItem, CustomerOrder, long)>(new Error("OrderItem.AlreadyCancelled", "Itens cancelados não podem ser transferidos."));

        var targetOrder = await _orderRepository.GetByIdForUpdateAsync(request.TargetCustomerOrderId, cancellationToken);
        if (targetOrder is null || !targetOrder.IsActive)
            return Result.Failure<(CustomerOrder, OrderItem, CustomerOrder, long)>(new Error("CustomerOrder.TargetNotFound", "Target order not found."));

        return Result.Success((sourceOrder, itemToTransfer, targetOrder, itemToTransfer.OrderItemStatusId));
    }

    private async Task<Result<long>> ApplyTransferAsync(
        TransferTableItemCommand request,
        (CustomerOrder SourceOrder, OrderItem ItemToTransfer, CustomerOrder TargetOrder, long OriginalStatusId) context,
        CancellationToken cancellationToken)
    {
        var (sourceOrder, itemToTransfer, targetOrder, originalStatusId) = context;
        var currentTime = _timeProvider.GetLocalNow().DateTime;

        // 1. Cancela o item na origem para a transferência (bypassa status final com segurança)
        var cancelResult = sourceOrder.ForceCancelItemForTransfer(itemToTransfer.Id, currentTime, request.ActorEmployeeId);
        if (cancelResult.IsFailure)
            return Result.Failure<long>(cancelResult.Error);

        // 2. Adiciona o item no destino (ele nasce com status Lançado)
        var addResult = targetOrder.AddItem(
            itemToTransfer.ProductId,
            itemToTransfer.UnitPrice,
            itemToTransfer.Quantity,
            itemToTransfer.Notes,
            request.ActorEmployeeId,
            currentTime);

        if (addResult.IsFailure)
            return Result.Failure<long>(addResult.Error);

        // 3. Restaura o status original no destino (permite recuperar o status Entregue caso necessário)
        var newlyAddedItem = targetOrder.Items.Last();
        if (newlyAddedItem.OrderItemStatusId != originalStatusId)
        {
            var statusResult = targetOrder.UpdateItemStatus(newlyAddedItem.Id, originalStatusId, currentTime, request.ActorEmployeeId);
            if (statusResult.IsFailure)
                return Result.Failure<long>(statusResult.Error);
        }

        var transferResult = TableItemTransfer.Create(
            request.SourceCustomerOrderId,
            request.CustomerOrderItemId,
            request.SourceDiningTableId,
            request.TargetDiningTableId,
            request.ActorEmployeeId);

        if (transferResult.IsFailure)
            return Result.Failure<long>(transferResult.Error);

        await _transferRepository.AddAsync(transferResult.Value, cancellationToken);

        // 4. Valida se restou algum item ativo na mesa de origem após transferir este item único
        bool hasActiveItems = sourceOrder.Items.Any(i => i.OrderItemStatusId != OrderItemStatusIds.Cancelado);

        var sourceTable = await _diningTableRepository.GetByIdForUpdateAsync(request.SourceDiningTableId, cancellationToken);
        if (sourceTable is not null)
        {
            if (!hasActiveItems)
            {
                // Se a mesa ficou completamente vazia, libera ela e cancela o pedido
                sourceTable.SetAvailable();
                var cancelOrderResult = sourceOrder.Cancel(currentTime);
                if (cancelOrderResult.IsFailure)
                    return Result.Failure<long>(cancelOrderResult.Error);
            }
            else
            {
                // Se ainda sobrou algum item na mesa, garante que ela continue ocupada
                sourceTable.SetInUse();
            }
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success(transferResult.Value.Id);
    }
}