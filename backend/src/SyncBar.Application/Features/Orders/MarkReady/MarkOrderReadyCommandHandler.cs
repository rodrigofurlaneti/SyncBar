using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.MarkReady;

internal sealed class MarkOrderReadyCommandHandler(
    ICustomerOrderRepository orderRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : BaseCommandHandler<MarkOrderReadyCommand>(logRepository, unitOfWork)
{
    public override Task<Result> Handle(MarkOrderReadyCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(MarkOrderReadyCommandHandler), nameof(Handle), null, async _ =>
        {
            var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
            if (order is null || !order.IsActive)
                return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

            var result = order.MarkReadyForDispatch(timeProvider.GetLocalNow().DateTime);
            if (result.IsFailure) return result;

            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        });
}
