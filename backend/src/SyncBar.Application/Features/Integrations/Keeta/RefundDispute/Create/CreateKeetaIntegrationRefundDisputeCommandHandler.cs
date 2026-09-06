using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create
{
    internal sealed class CreateKeetaIntegrationRefundDisputeCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationRefundDisputeCommand, CreateKeetaIntegrationRefundDisputeResponse>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationRefundDisputeCommandHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationRefundDisputeResponse>> Handle(
            CreateKeetaIntegrationRefundDisputeCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationRefundDisputeCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _disputeRepository.ExistsByAfterSaleOrderIdAsync(
                        request.AfterSaleOrderId, cancellationToken);

                    if (exists)
                    {
                        return Result.Failure<CreateKeetaIntegrationRefundDisputeResponse>(
                            Error.Conflict(
                                "KeetaRefundDispute.AlreadyExists",
                                $"Já existe uma disputa de reembolso Keeta cadastrada para o AfterSaleOrderId {request.AfterSaleOrderId}."));
                    }

                    var disputeResult = KeetaIntegrationRefundDispute.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.OrderId,
                        request.AfterSaleOrderId,
                        request.RefundAmount,
                        request.ApplyReason);

                    if (disputeResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationRefundDisputeResponse>(disputeResult.Error);

                    var dispute = disputeResult.Value;

                    await _disputeRepository.AddAsync(dispute, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationRefundDisputeResponse(
                        dispute.Id,
                        dispute.CompanyId,
                        dispute.BranchId,
                        dispute.OrderId,
                        dispute.AfterSaleOrderId,
                        dispute.RefundAmount,
                        dispute.ResolutionStatus);

                    return Result.Success(response);
                });
        }
    }
}
