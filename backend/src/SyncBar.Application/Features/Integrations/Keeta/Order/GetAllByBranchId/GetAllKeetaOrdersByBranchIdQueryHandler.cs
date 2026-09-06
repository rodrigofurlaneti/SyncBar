using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId
{
    internal sealed class GetAllKeetaOrdersByBranchIdQueryHandler
        : BaseQueryHandler<GetAllKeetaOrdersByBranchIdQuery, IReadOnlyList<KeetaIntegrationOrderResponse>>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;

        public GetAllKeetaOrdersByBranchIdQueryHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
        }

        public override async Task<Result<IReadOnlyList<KeetaIntegrationOrderResponse>>> Handle(
            GetAllKeetaOrdersByBranchIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllKeetaOrdersByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var orders = await _orderRepository.GetAllByBranchIdAsync(request.BranchId, cancellationToken);

                    var response = orders
                        .Select(o => new KeetaIntegrationOrderResponse(
                            o.Id, o.CompanyId, o.BranchId, o.CustomerId, o.CustomerOrderId,
                            o.KeetaOrderId, o.DisplayId, o.InternalMerchantId, o.KeetaMerchantId,
                            o.Status, o.OrderType, o.DeliveredBy, o.OrderAmount, o.Currency,
                            o.RawOrderJson, o.OrderCreatedAtUtc, o.CreatedAtUtc, o.ConfirmedAtUtc,
                            o.ReadyForPickupAtUtc, o.ConcludedAtUtc))
                        .ToList();

                    return Result.Success<IReadOnlyList<KeetaIntegrationOrderResponse>>(response);
                });
        }
    }
}
