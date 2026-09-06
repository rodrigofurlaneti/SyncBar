using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder
{
    internal sealed class DispatchKeetaOrderCommandHandler : BaseCommandHandler<DispatchKeetaOrderCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaOrderClient _orderClient;
        private readonly IUnitOfWork _unitOfWork;

        public DispatchKeetaOrderCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            IKeetaOrderClient orderClient,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _orderClient = orderClient;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(DispatchKeetaOrderCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DispatchKeetaOrderCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    var trackingEvent = string.IsNullOrWhiteSpace(request.TrackingEventType)
                        ? null
                        : new KeetaDeliveryTrackingEvent(request.TrackingEventType, DateTime.UtcNow, request.TrackingEventMessage);

                    await _orderClient.DispatchOrderAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, trackingEvent, cancellationToken);

                    order.ChangeStatus("DISPATCHED");
                    _orderRepository.Update(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
