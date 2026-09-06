using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByDisplayId
{
    internal sealed class GetKeetaOrderByDisplayIdQueryHandler
        : BaseQueryHandler<GetKeetaOrderByDisplayIdQuery, KeetaIntegrationOrderResponse>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;

        public GetKeetaOrderByDisplayIdQueryHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
        }

        public override async Task<Result<KeetaIntegrationOrderResponse>> Handle(
            GetKeetaOrderByDisplayIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaOrderByDisplayIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var order = await _orderRepository.GetByDisplayIdAsync(request.DisplayId, cancellationToken);

                    if (order is null)
                    {
                        return Result.Failure<KeetaIntegrationOrderResponse>(
                            Error.NotFound(
                                "KeetaOrder.NotFound",
                                $"Pedido Keeta com DisplayId {request.DisplayId} não foi encontrado."));
                    }

                    var response = new KeetaIntegrationOrderResponse(
                        order.Id,
                        order.CompanyId,
                        order.BranchId,
                        order.CustomerId,
                        order.CustomerOrderId,
                        order.KeetaOrderId,
                        order.DisplayId,
                        order.InternalMerchantId,
                        order.KeetaMerchantId,
                        order.Status,
                        order.OrderType,
                        order.DeliveredBy,
                        order.OrderAmount,
                        order.Currency,
                        order.RawOrderJson,
                        order.OrderCreatedAtUtc,
                        order.CreatedAtUtc,
                        order.ConfirmedAtUtc,
                        order.ReadyForPickupAtUtc,
                        order.ConcludedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
