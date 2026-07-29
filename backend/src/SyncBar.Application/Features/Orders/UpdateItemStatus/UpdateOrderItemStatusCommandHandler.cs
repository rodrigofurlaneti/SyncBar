using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

internal sealed class UpdateOrderItemStatusCommandHandler(
    ICustomerOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) 
    : ICommandHandler<UpdateOrderItemStatusCommand>
{
    public async Task<Result> Handle(UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
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

        // 2. CAPTURA A HORA ATUAL DO TIMEPROVIDER
        var currentTime = timeProvider.GetLocalNow().DateTime;

        // 3. PASSA NA ORDEM CORRETA: ID do Item, Status, Data/Hora, e por fim o Funcionário
        var result = order.UpdateItemStatus(request.OrderItemId, request.OrderItemStatusId, currentTime, request.ActorEmployeeId);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}