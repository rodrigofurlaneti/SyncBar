using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Reopen;

internal sealed class ReopenOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ReopenOrderCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(ReopenOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ReopenOrderCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável pela reabertura, preencha:
                // userIdBox.Value = request.UserId; 

                var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = timeProvider.GetLocalNow().DateTime;

                var result = order.ReopenForConsumption(currentTime);
                if (result.IsFailure)
                    return result;

                if (order.DiningTableId.HasValue)
                {
                    var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                    table?.ChangeStatus(TableStatusIds.Ocupada);
                }

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}