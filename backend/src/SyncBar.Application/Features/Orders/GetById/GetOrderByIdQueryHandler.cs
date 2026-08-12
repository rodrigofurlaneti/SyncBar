using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.GetById;

internal sealed class GetOrderByIdQueryHandler(
    ICustomerOrderRepository orderRepository,
    IOrderPartialPaymentRepository partialPaymentRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetOrderByIdQuery, OrderResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetOrderByIdQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, se possuir
            async (userIdBox) =>
            {
                // Se houver o ID do usuário que fez a requisição, preencha:
                // userIdBox.Value = request.UserId;

                var order = await orderRepository.GetByIdAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure<OrderResponse>(new Error("CustomerOrder.NotFound", "Order not found."));

                var partials = await partialPaymentRepository.GetByOrderAsync(order.Id, cancellationToken);
                return Result.Success(order.ToResponse(partials.Sum(p => p.Amount)));
            });
    }
}