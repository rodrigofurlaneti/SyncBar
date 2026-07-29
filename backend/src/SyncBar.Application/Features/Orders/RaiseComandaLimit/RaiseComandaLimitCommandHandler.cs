using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.RaiseComandaLimit;

internal sealed class RaiseComandaLimitCommandHandler(
    ICustomerOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) 
    : ICommandHandler<RaiseComandaLimitCommand>
{
    public async Task<Result> Handle(RaiseComandaLimitCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var currentTime = timeProvider.GetLocalNow().DateTime;

        var result = order.RaiseCreditLimit(request.NewLimitAmount, currentTime);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}