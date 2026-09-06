using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund
{
    internal sealed class RejectKeetaOrderRefundCommandHandler : BaseCommandHandler<RejectKeetaOrderRefundCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;
        private readonly IKeetaOrderClient _orderClient;
        private readonly IUnitOfWork _unitOfWork;

        public RejectKeetaOrderRefundCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            IKeetaOrderClient orderClient,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _disputeRepository = disputeRepository;
            _orderClient = orderClient;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(RejectKeetaOrderRefundCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RejectKeetaOrderRefundCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken);
                    if (order is null)
                        return Result.Failure(Error.NotFound("KeetaOrder.NotFound", "Pedido Keeta não encontrado."));

                    await _orderClient.RejectRefundAsync(order.CompanyId, order.BranchId, order.KeetaOrderId, request.Reason, request.Code, cancellationToken);

                    order.ChangeStatus("REFUND_REJECTED");
                    _orderRepository.Update(order);

                    var dispute = await _disputeRepository.GetByOrderIdAsync(order.KeetaOrderId, cancellationToken);
                    if (dispute is not null)
                    {
                        dispute.Resolve(accepted: false, request.Code, request.Reason);
                        _disputeRepository.Update(dispute);
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
