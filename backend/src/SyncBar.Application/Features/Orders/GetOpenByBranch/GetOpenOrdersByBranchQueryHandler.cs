using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.GetOpenByBranch;

internal sealed class GetOpenOrdersByBranchQueryHandler(ICustomerOrderRepository orderRepository)
    : IQueryHandler<GetOpenOrdersByBranchQuery, IReadOnlyCollection<OrderResponse>>
{
    public async Task<Result<IReadOnlyCollection<OrderResponse>>> Handle(
        GetOpenOrdersByBranchQuery request, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);

        // Ordenacao em C# — nunca ORDER BY em SqlQuery.
        IReadOnlyCollection<OrderResponse> response = orders
            .OrderBy(o => o.OpenedAt)
            .Select(o => o.ToResponse())
            .ToList();

        return Result.Success(response);
    }
}
