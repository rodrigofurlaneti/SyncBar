using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.GetById;

internal sealed class GetOrderByIdQueryHandler(
    ICustomerOrderRepository orderRepository,
    IOrderPartialPaymentRepository partialPaymentRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<OrderResponse>(new Error("CustomerOrder.NotFound", "Order not found."));

        var partials = await partialPaymentRepository.GetByOrderAsync(order.Id, cancellationToken);
        return Result.Success(order.ToResponse(partials.Sum(p => p.Amount)));
    }
}
