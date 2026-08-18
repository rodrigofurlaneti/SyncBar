using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

internal sealed class UpdateOrderItemStatusCommandHandler : BaseCommandHandler<UpdateOrderItemStatusCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderItemStatusCommandHandler(
        ICustomerOrderRepository orderRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateOrderItemStatusCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Associa o Id do funcionário/usuário que está atualizando o status do item ao log de auditoria
                userIdBox.Value = request.ActorEmployeeId;

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                // Antifraude: cancelar item que JA FOI para a cozinha exige gerente.
                if (request.OrderItemStatusId == Domain.Constants.OrderItemStatusIds.Cancelado && !request.IsManager)
                {
                    var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                    if (item is not null && item.OrderItemStatusId != Domain.Constants.OrderItemStatusIds.Lancado)
                        return Result.Failure(new Error("OrderItem.CancelRequiresManager",
                            "Item já enviado à cozinha — somente o gerente pode cancelar."));
                }

                // 2. CAPTURA A HORA ATUAL DO TimeProviderCustom
                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                // 3. PASSA NA ORDEM CORRETA: ID do Item, Status, Data/Hora, e por fim o Funcionário
                var result = order.UpdateItemStatus(request.OrderItemId, request.OrderItemStatusId, currentTime, request.ActorEmployeeId);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}