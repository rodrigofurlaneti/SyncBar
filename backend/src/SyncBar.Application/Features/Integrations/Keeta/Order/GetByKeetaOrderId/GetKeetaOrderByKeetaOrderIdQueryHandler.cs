using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId
{
    internal sealed class GetKeetaOrderByKeetaOrderIdQueryHandler
        : BaseQueryHandler<GetKeetaOrderByKeetaOrderIdQuery, KeetaIntegrationOrderResponse>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;

        public GetKeetaOrderByKeetaOrderIdQueryHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
        }

        public override async Task<Result<KeetaIntegrationOrderResponse>> Handle(
            GetKeetaOrderByKeetaOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaOrderByKeetaOrderIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var order = await _orderRepository.GetByKeetaOrderIdAsync(request.KeetaOrderId, cancellationToken);

                    if (order is null)
                    {
                        return Result.Failure<KeetaIntegrationOrderResponse>(
                            Error.NotFound(
                                "KeetaOrder.NotFound",
                                $"Pedido Keeta com KeetaOrderId {request.KeetaOrderId} não foi encontrado."));
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
