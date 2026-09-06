using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate
{
    internal sealed class SendKeetaOrderTrackingUpdateCommandHandler : BaseCommandHandler<SendKeetaOrderTrackingUpdateCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaOrderClient _orderClient;

        public SendKeetaOrderTrackingUpdateCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            IKeetaOrderClient orderClient,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _orderClient = orderClient;
        }

        public override async Task<Result> Handle(SendKeetaOrderTrackingUpdateCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(SendKeetaOrderTrackingUpdateCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    var trackingEvent = new KeetaDeliveryTrackingEvent(request.TrackingEventType, DateTime.UtcNow, request.TrackingEventMessage);

                    await _orderClient.SendTrackingUpdateAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, trackingEvent, cancellationToken);

                    return Result.Success();
                });
        }
    }
}
