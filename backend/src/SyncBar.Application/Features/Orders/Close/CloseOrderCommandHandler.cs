using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Close;

internal sealed class CloseOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    IServiceFeeSettingRepository serviceFeeSettingRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseOrderCommand>
{
    public async Task<Result> Handle(CloseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        // A filial pode ter a taxa de servico DESLIGADA (eventos sem 10%):
        // nesse caso a conta fecha com 0% e a taxa nem aparece na impressao.
        var feeSetting = await serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, cancellationToken);
        var serviceFeeEnabled = feeSetting?.Enabled ?? true;
        var effectiveServiceFeeRate = serviceFeeEnabled ? request.ServiceFeeRate : 0m;

        var result = order.Close(effectiveServiceFeeRate);
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
