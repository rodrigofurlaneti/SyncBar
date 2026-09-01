using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class GetIfoodOrdersQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    ICustomerOrderRepository customerOrderRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOrdersQuery, IReadOnlyCollection<IfoodOrderResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodOrderResponse>>> Handle(
        GetIfoodOrdersQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOrdersQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrders = await IfoodOrderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (IfoodOrders.Count == 0)
                    return Result.Success<IReadOnlyCollection<IfoodOrderResponse>>([]);

                var customerOrders = await customerOrderRepository.GetByIdsAsync(
                    IfoodOrders.Select(x => x.CustomerOrderId).ToList(), cancellationToken);
                var customerOrdersById = customerOrders.ToDictionary(x => x.Id);

                IReadOnlyCollection<IfoodOrderResponse> responses = IfoodOrders
                    .Select(io =>
                    {
                        customerOrdersById.TryGetValue(io.CustomerOrderId, out var co);
                        return new IfoodOrderResponse(
                            io.Id, io.CustomerOrderId, io.IfoodOrderId, io.DisplayId, io.IfoodOrderType, io.DeliveredBy,
                            io.OrderTiming, io.PreparationStartDateTime, io.Status,
                            io.ConfirmDeadlineAt, io.ConfirmedAt, io.HasUnmappedItems,
                            co?.CustomerName ?? "Cliente Ifood", co?.CustomerPhone, co?.DeliveryAddress,
                            co?.TotalAmount ?? 0m, io.CreatedAt);
                    })
                    .OrderBy(x => x.CreatedAt)
                    .ToList();

                return Result.Success(responses);
            });
    }
}
