using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered
{
    internal sealed class MarkKeetaOrderDeliveredCommandHandler : BaseCommandHandler<MarkKeetaOrderDeliveredCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaOrderClient _orderClient;
        private readonly IUnitOfWork _unitOfWork;

        public MarkKeetaOrderDeliveredCommandHandler(
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

        public override async Task<Result> Handle(MarkKeetaOrderDeliveredCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(MarkKeetaOrderDeliveredCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    await _orderClient.MarkDeliveredAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, cancellationToken);

                    order.MarkAsConcluded();
                    _orderRepository.Update(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
