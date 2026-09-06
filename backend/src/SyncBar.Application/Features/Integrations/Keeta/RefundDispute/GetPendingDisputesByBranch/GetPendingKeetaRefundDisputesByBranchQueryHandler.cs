using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch
{
    internal sealed class GetPendingKeetaRefundDisputesByBranchQueryHandler
        : BaseQueryHandler<GetPendingKeetaRefundDisputesByBranchQuery, IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;

        public GetPendingKeetaRefundDisputesByBranchQueryHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
        }

        public override async Task<Result<IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>> Handle(
            GetPendingKeetaRefundDisputesByBranchQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetPendingKeetaRefundDisputesByBranchQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var disputes = await _disputeRepository.GetPendingDisputesByBranchAsync(request.BranchId, cancellationToken);

                    var response = disputes
                        .Select(d => new KeetaIntegrationRefundDisputeResponse(
                            d.Id, d.CompanyId, d.BranchId, d.OrderId, d.AfterSaleOrderId, d.RefundAmount,
                            d.Currency, d.ApplyReason, d.ResolutionStatus, d.DenialReasonCode, d.DenialReasonText,
                            d.ReceivedAtUtc, d.ResolvedAtUtc))
                        .ToList();

                    return Result.Success<IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>(response);
                });
        }
    }
}
