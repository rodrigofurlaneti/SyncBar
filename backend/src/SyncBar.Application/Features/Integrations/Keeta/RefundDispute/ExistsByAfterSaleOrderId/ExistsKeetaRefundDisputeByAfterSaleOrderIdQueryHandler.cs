using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId
{
    internal sealed class ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandler
        : BaseQueryHandler<ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery, bool>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;

        public ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsKeetaRefundDisputeByAfterSaleOrderIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _disputeRepository.ExistsByAfterSaleOrderIdAsync(request.AfterSaleOrderId, cancellationToken);
                    return Result.Success(exists);
                });
        }
    }
}
