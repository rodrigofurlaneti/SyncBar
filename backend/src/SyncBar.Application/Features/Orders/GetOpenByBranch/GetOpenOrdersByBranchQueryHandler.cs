using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.GetOpenByBranch;

internal sealed class GetOpenOrdersByBranchQueryHandler(
    ICustomerOrderRepository orderRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetOpenOrdersByBranchQuery, IReadOnlyCollection<OrderResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<OrderResponse>>> Handle(
        GetOpenOrdersByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetOpenOrdersByBranchQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var orders = await orderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                IReadOnlyCollection<OrderResponse> response = orders
                    .OrderBy(o => o.OpenedAt)
                    .Select(o => o.ToResponse())
                    .ToList();
                return Result.Success(response);
            });
    }
}