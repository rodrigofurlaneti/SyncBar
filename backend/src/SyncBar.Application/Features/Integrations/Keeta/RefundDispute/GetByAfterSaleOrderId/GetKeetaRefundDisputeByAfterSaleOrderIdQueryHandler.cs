using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId
{
    internal sealed class GetKeetaRefundDisputeByAfterSaleOrderIdQueryHandler
        : BaseQueryHandler<GetKeetaRefundDisputeByAfterSaleOrderIdQuery, KeetaIntegrationRefundDisputeResponse>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;

        public GetKeetaRefundDisputeByAfterSaleOrderIdQueryHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
        }

        public override async Task<Result<KeetaIntegrationRefundDisputeResponse>> Handle(
            GetKeetaRefundDisputeByAfterSaleOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaRefundDisputeByAfterSaleOrderIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var dispute = await _disputeRepository.GetByAfterSaleOrderIdAsync(request.AfterSaleOrderId, cancellationToken);

                    if (dispute is null)
                    {
                        return Result.Failure<KeetaIntegrationRefundDisputeResponse>(
                            Error.NotFound(
                                "KeetaRefundDispute.NotFound",
                                $"Disputa de reembolso Keeta com AfterSaleOrderId {request.AfterSaleOrderId} não foi encontrada."));
                    }

                    var response = new KeetaIntegrationRefundDisputeResponse(
                        dispute.Id,
                        dispute.CompanyId,
                        dispute.BranchId,
                        dispute.OrderId,
                        dispute.AfterSaleOrderId,
                        dispute.RefundAmount,
                        dispute.Currency,
                        dispute.ApplyReason,
                        dispute.ResolutionStatus,
                        dispute.DenialReasonCode,
                        dispute.DenialReasonText,
                        dispute.ReceivedAtUtc,
                        dispute.ResolvedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
