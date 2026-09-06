using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation
{
    internal sealed class RequestKeetaOrderCancellationCommandHandler : BaseCommandHandler<RequestKeetaOrderCancellationCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaOrderClient _orderClient;
        private readonly IUnitOfWork _unitOfWork;

        public RequestKeetaOrderCancellationCommandHandler(
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

        public override async Task<Result> Handle(RequestKeetaOrderCancellationCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RequestKeetaOrderCancellationCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    await _orderClient.RequestCancellationAsync(
                        order.CompanyId,
                        order.BranchId,
                        order.KeetaOrderId,
                        request.Reason,
                        request.Code,
                        request.Mode,
                        request.OutOfStockItems,
                        request.InvalidItems,
                        cancellationToken);

                    order.ChangeStatus("CANCELLATION_REQUESTED");
                    _orderRepository.Update(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
