using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.RemoveServiceFee;

internal sealed class RemoveServiceFeeCommandHandler(
    ICustomerOrderRepository orderRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RemoveServiceFeeCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(RemoveServiceFeeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RemoveServiceFeeCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável por remover a taxa, preencha:
                // userIdBox.Value = request.UserId; 

                var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = timeProvider.GetLocalNow().DateTime;

                var result = order.RemoveServiceFee(currentTime);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}