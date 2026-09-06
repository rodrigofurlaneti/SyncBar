using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete
{
    internal sealed class DeleteKeetaIntegrationOrderEventLogCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationOrderEventLogCommand>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationOrderEventLogCommandHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationOrderEventLogCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationOrderEventLogCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var eventLog = await _eventLogRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (eventLog is null || eventLog.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaOrderEventLog.NotFound",
                                $"Evento Keeta com ID {request.Id} não foi encontrado para esta empresa."));
                    }

                    _eventLogRepository.Delete(eventLog);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
