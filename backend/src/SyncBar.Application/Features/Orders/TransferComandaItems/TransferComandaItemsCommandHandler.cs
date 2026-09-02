using MediatR;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Orders.TransferComandaAllItem;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.TransferComandaItems
{
    internal sealed class TransferComandaItemsCommandHandler : BaseCommandHandler<TransferComandaItemsCommand, Unit>
    {
        private readonly ICustomerOrderRepository _orderRepository;
        private readonly IComandaItemTransferRepository _transferRepository;
        private readonly IComandaRepository _comandaRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public TransferComandaItemsCommandHandler(
            ICustomerOrderRepository orderRepository,
            IComandaItemTransferRepository transferRepository,
            IComandaRepository comandaRepository,
            TimeProvider timeProvider,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _transferRepository = transferRepository;
            _comandaRepository = comandaRepository;
            _timeProvider = timeProvider;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<Unit>> Handle(TransferComandaItemsCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(TransferComandaItemsCommandHandler),
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

                    // Ordena os itens selecionados de forma crescente para processamento limpo
                    var sortedItemIds = request.CustomerOrderItemIds.OrderBy(id => id).ToList();

                    foreach (var itemId in sortedItemIds)
                    {
                        var itemToTransfer = sourceOrder.Items.FirstOrDefault(i => i.Id == itemId);
                        if (itemToTransfer is null)
                            return Result.Failure<Unit>(new Error("CustomerOrderItem.NotFound", $"Item {itemId} not found in source order."));

                        if (itemToTransfer.OrderItemStatusId == OrderItemStatusIds.Cancelado)
                            return Result.Failure<Unit>(new Error("OrderItem.AlreadyCancelled", "Itens cancelados não podem ser transferidos."));

                        var originalStatusId = itemToTransfer.OrderItemStatusId;

                        // Cancela na origem para transferência (bypassa status final com segurança)
                        var cancelResult = sourceOrder.ForceCancelItemForTransfer(itemToTransfer.Id, currentTime, request.ActorEmployeeId);
                        if (cancelResult.IsFailure)
                            return Result.Failure<Unit>(cancelResult.Error);

                        // Adiciona no destino já com o status original preservado (evita reprocurar o item
                        // recém-criado por Id, que ainda é 0 até o SaveChanges e colide entre itens do mesmo lote)
                        var addResult = targetOrder.AddTransferredItem(
                            itemToTransfer.ProductId,
                            itemToTransfer.UnitPrice,
                            itemToTransfer.Quantity,
                            itemToTransfer.Notes,
                            request.ActorEmployeeId,
                            originalStatusId,
                            currentTime);

                        if (addResult.IsFailure)
                            return Result.Failure<Unit>(addResult.Error);

                        var transferResult = ComandaItemTransfer.Create(
                            request.SourceCustomerOrderId,
                            itemId,
                            request.SourceComandaId,
                            request.TargetComandaId,
                            request.ActorEmployeeId);

                        if (transferResult.IsFailure)
                            return Result.Failure<Unit>(transferResult.Error);

                        await _transferRepository.AddAsync(transferResult.Value, cancellationToken);
                    }

                    // Verifica se ainda sobrou algum produto ativo na comanda de origem
                    bool hasActiveItems = sourceOrder.Items.Any(i => i.OrderItemStatusId != OrderItemStatusIds.Cancelado);

                    var sourceComanda = await _comandaRepository.GetByIdForUpdateAsync(request.SourceComandaId, cancellationToken);
                    if (sourceComanda is not null)
                    {
                        if (!hasActiveItems)
                        {
                            // Se não sobrou nada, libera a comanda e cancela o pedido vazio
                            sourceComanda.SetAvailable(); // Status 1: Disponível
                            var cancelOrderResult = sourceOrder.Cancel(currentTime);
                            if (cancelOrderResult.IsFailure)
                                return Result.Failure<Unit>(cancelOrderResult.Error);
                        }
                        else
                        {
                            // Se ainda há produtos, garante que a comanda permaneça em uso
                            sourceComanda.SetInUse(); // Status 2: Em Uso
                        }
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success(Unit.Value);
                });
        }
    }
}