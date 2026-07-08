using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Cancel;

internal sealed class CancelOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    IComandaRepository comandaRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var result = order.Cancel();
        if (result.IsFailure)
            return result;

        // Libera mesa e comanda.
        if (order.DiningTableId.HasValue)
        {
            var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
            table?.ChangeStatus(TableStatusIds.Livre);
        }

        if (order.ComandaId.HasValue)
        {
            var comanda = await comandaRepository.GetByIdForUpdateAsync(order.ComandaId.Value, cancellationToken);
            comanda?.ChangeStatus(ComandaStatusIds.Disponivel);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
