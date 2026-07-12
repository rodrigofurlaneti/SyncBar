using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Reopen;

internal sealed class ReopenOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReopenOrderCommand>
{
    public async Task<Result> Handle(ReopenOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        // Fechou a conta por engano: volta a EmAndamento (taxa recalcula no proximo fechamento).
        var result = order.ReopenForConsumption();
        if (result.IsFailure)
            return result;

        if (order.DiningTableId.HasValue)
        {
            var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
            table?.ChangeStatus(TableStatusIds.Ocupada);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
