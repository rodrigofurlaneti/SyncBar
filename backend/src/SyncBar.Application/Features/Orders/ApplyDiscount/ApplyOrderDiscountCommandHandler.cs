using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.ApplyDiscount;

internal sealed class ApplyOrderDiscountCommandHandler(
    ICustomerOrderRepository orderRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ApplyOrderDiscountCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(ApplyOrderDiscountCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ApplyOrderDiscountCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/funcionário responsável pela ação, preencha:
                // userIdBox.Value = request.EmployeeId; // ou request.UserId

                var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = timeProvider.GetLocalNow().DateTime;

                var result = order.ApplyDiscount(request.DiscountAmount, currentTime);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}