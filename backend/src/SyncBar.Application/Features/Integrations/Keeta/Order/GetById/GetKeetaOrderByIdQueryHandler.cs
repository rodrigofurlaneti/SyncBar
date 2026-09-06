using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetById
{
    internal sealed class GetKeetaOrderByIdQueryHandler
        : BaseQueryHandler<GetKeetaOrderByIdQuery, KeetaIntegrationOrderResponse>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;

        public GetKeetaOrderByIdQueryHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
        }

        public override async Task<Result<KeetaIntegrationOrderResponse>> Handle(
            GetKeetaOrderByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaOrderByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (order is null)
                    {
                        return Result.Failure<KeetaIntegrationOrderResponse>(
                            Error.NotFound(
                                "KeetaOrder.NotFound",
                                $"Pedido Keeta com ID {request.Id} não foi encontrado."));
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
