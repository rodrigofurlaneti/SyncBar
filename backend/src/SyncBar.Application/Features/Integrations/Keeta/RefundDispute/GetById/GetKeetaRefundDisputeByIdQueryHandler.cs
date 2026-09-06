using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById
{
    internal sealed class GetKeetaRefundDisputeByIdQueryHandler
        : BaseQueryHandler<GetKeetaRefundDisputeByIdQuery, KeetaIntegrationRefundDisputeResponse>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;

        public GetKeetaRefundDisputeByIdQueryHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
        }

        public override async Task<Result<KeetaIntegrationRefundDisputeResponse>> Handle(
            GetKeetaRefundDisputeByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaRefundDisputeByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var dispute = await _disputeRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (dispute is null)
                    {
                        return Result.Failure<KeetaIntegrationRefundDisputeResponse>(
                            Error.NotFound(
                                "KeetaRefundDispute.NotFound",
                                $"Disputa de reembolso Keeta com ID {request.Id} não foi encontrada."));
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
