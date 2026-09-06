using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update
{
    internal sealed class UpdateKeetaIntegrationRefundDisputeCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationRefundDisputeCommand>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationRefundDisputeCommandHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationRefundDisputeCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationRefundDisputeCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var dispute = await _disputeRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (dispute is null || dispute.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaRefundDispute.NotFound",
                                $"Disputa de reembolso Keeta com ID {request.Id} não foi encontrada para esta empresa."));
                    }

                    dispute.Resolve(request.Accepted, request.DenialReasonCode, request.DenialReasonText);

                    _disputeRepository.Update(dispute);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
