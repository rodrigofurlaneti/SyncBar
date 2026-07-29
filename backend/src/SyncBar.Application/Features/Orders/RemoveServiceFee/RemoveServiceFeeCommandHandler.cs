using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.RemoveServiceFee;

internal sealed class RemoveServiceFeeCommandHandler(
    ICustomerOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) 
    : ICommandHandler<RemoveServiceFeeCommand>
{
    public async Task<Result> Handle(RemoveServiceFeeCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var currentTime = timeProvider.GetLocalNow().DateTime;

        var result = order.RemoveServiceFee(currentTime);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}