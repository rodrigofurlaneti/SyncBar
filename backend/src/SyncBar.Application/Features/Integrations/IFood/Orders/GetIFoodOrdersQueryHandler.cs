using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class GetIFoodOrdersQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    ICustomerOrderRepository customerOrderRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOrdersQuery, IReadOnlyCollection<IFoodOrderResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodOrderResponse>>> Handle(
        GetIFoodOrdersQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOrdersQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrders = await ifoodOrderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (ifoodOrders.Count == 0)
                    return Result.Success<IReadOnlyCollection<IFoodOrderResponse>>([]);

                var customerOrders = await customerOrderRepository.GetByIdsAsync(
                    ifoodOrders.Select(x => x.CustomerOrderId).ToList(), cancellationToken);
                var customerOrdersById = customerOrders.ToDictionary(x => x.Id);

                IReadOnlyCollection<IFoodOrderResponse> responses = ifoodOrders
                    .Select(io =>
                    {
                        customerOrdersById.TryGetValue(io.CustomerOrderId, out var co);
                        return new IFoodOrderResponse(
                            io.Id, io.CustomerOrderId, io.IFoodOrderId, io.DisplayId, io.IFoodOrderType, io.DeliveredBy,
                            io.OrderTiming, io.PreparationStartDateTime, io.Status,
                            io.ConfirmDeadlineAt, io.ConfirmedAt, io.HasUnmappedItems,
                            co?.CustomerName ?? "Cliente iFood", co?.CustomerPhone, co?.DeliveryAddress,
                            co?.TotalAmount ?? 0m, io.CreatedAt);
                    })
                    .OrderBy(x => x.CreatedAt)
                    .ToList();

                return Result.Success(responses);
            });
    }
}
