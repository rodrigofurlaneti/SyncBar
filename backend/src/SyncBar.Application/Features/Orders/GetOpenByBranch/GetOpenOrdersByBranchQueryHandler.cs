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
            null, // Substitua pelo IP presente no request, caso possua
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:

                var orders = await orderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);

                // Ordenacao em C# — nunca ORDER BY em SqlQuery.
                IReadOnlyCollection<OrderResponse> response = orders
                    .OrderBy(o => o.OpenedAt)
                    .Select(o => o.ToResponse())
                    .ToList();

                return Result.Success(response);
            });
    }
}