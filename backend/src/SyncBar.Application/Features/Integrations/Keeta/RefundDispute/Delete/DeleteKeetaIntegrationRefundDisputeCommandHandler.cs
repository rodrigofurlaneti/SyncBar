using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete
{
    internal sealed class DeleteKeetaIntegrationRefundDisputeCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationRefundDisputeCommand>
    {
        private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationRefundDisputeCommandHandler(
            IKeetaIntegrationRefundDisputeRepository disputeRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _disputeRepository = disputeRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationRefundDisputeCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationRefundDisputeCommandHandler),
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

                    _disputeRepository.Delete(dispute);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
