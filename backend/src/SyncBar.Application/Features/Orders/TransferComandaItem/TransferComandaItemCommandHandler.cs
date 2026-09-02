using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.TransferComandaItem
{
    internal sealed class TransferComandaItemCommandHandler : BaseCommandHandler<TransferComandaItemCommand, long>
    {
        private readonly ICustomerOrderRepository _orderRepository;
        private readonly IComandaItemTransferRepository _transferRepository;
        private readonly IComandaRepository _comandaRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public TransferComandaItemCommandHandler(
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

        public override async Task<Result<long>> Handle(TransferComandaItemCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(TransferComandaItemCommandHandler),
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
            TransferComandaItemCommand request, CancellationToken cancellationToken)
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
            TransferComandaItemCommand request,
            (CustomerOrder SourceOrder, OrderItem ItemToTransfer, CustomerOrder TargetOrder, long OriginalStatusId) context,
            CancellationToken cancellationToken)
        {
            var (sourceOrder, itemToTransfer, targetOrder, originalStatusId) = context;
            var currentTime = _timeProvider.GetLocalNow().DateTime;

            var cancelResult = sourceOrder.ForceCancelItemForTransfer(itemToTransfer.Id, currentTime, request.ActorEmployeeId);
            if (cancelResult.IsFailure)
                return Result.Failure<long>(cancelResult.Error);

            // Adiciona no destino já com o status original preservado (Lançado, Entregue etc.)
            var addResult = targetOrder.AddTransferredItem(
                itemToTransfer.ProductId,
                itemToTransfer.UnitPrice,
                itemToTransfer.Quantity,
                itemToTransfer.Notes,
                request.ActorEmployeeId,
                originalStatusId,
                currentTime);
            if (addResult.IsFailure)
                return Result.Failure<long>(addResult.Error);

            var transferResult = ComandaItemTransfer.Create(
                request.SourceCustomerOrderId,
                request.CustomerOrderItemId,
                request.SourceComandaId,
                request.TargetComandaId,
                request.ActorEmployeeId);
            if (transferResult.IsFailure)
                return Result.Failure<long>(transferResult.Error);

            await _transferRepository.AddAsync(transferResult.Value, cancellationToken);

            // Valida se restou algum item ativo na comanda de origem
            bool hasActiveItems = sourceOrder.Items.Any(i => i.OrderItemStatusId != OrderItemStatusIds.Cancelado);

            var sourceComanda = await _comandaRepository.GetByIdForUpdateAsync(request.SourceComandaId, cancellationToken);
            if (sourceComanda is not null)
            {
                if (!hasActiveItems)
                {
                    // Se não sobrou nenhum item, define a comanda como Disponível e cancela o pedido vazio
                    sourceComanda.SetAvailable();
                    var cancelOrderResult = sourceOrder.Cancel(currentTime);
                    if (cancelOrderResult.IsFailure)
                        return Result.Failure<long>(cancelOrderResult.Error);
                }
                else
                {
                    // Se ainda há produtos na comanda de origem, mantém ela Em Uso
                    sourceComanda.SetInUse();
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Success(transferResult.Value.Id);
        }
    }
}