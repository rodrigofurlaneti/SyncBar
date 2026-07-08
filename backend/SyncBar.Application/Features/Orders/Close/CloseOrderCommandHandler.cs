using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Close;

internal sealed class CloseOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseOrderCommand>
{
    public async Task<Result> Handle(CloseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var result = order.Close(request.ServiceFeeRate);
        if (result.IsFailure)
            return result;

        if (order.DiningTableId.HasValue)
        {
            var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
            table?.ChangeStatus(TableStatusIds.EmFechamento);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
