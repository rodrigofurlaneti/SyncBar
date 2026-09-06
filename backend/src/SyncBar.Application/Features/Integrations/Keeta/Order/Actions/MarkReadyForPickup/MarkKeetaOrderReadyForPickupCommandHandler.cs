using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup
{
    internal sealed class MarkKeetaOrderReadyForPickupCommandHandler : BaseCommandHandler<MarkKeetaOrderReadyForPickupCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaOrderClient _orderClient;
        private readonly IUnitOfWork _unitOfWork;

        public MarkKeetaOrderReadyForPickupCommandHandler(
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

        public override async Task<Result> Handle(MarkKeetaOrderReadyForPickupCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(MarkKeetaOrderReadyForPickupCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    await _orderClient.MarkReadyForPickupAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, cancellationToken);

                    order.MarkAsReadyForPickup();
                    _orderRepository.Update(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
