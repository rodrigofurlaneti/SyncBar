using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.RaiseComandaLimit;

internal sealed class RaiseComandaLimitCommandHandler(
    ICustomerOrderRepository orderRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RaiseComandaLimitCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(RaiseComandaLimitCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RaiseComandaLimitCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável pela liberação do limite, preencha:
                // userIdBox.Value = request.UserId; 

                var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = timeProvider.GetLocalNow().DateTime;

                var result = order.RaiseCreditLimit(request.NewLimitAmount, currentTime);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}