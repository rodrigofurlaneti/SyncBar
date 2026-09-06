using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update
{
    internal sealed class UpdateKeetaIntegrationOrderEventLogCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationOrderEventLogCommand>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationOrderEventLogCommandHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationOrderEventLogCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationOrderEventLogCommandHandler),
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

                    if (!string.IsNullOrWhiteSpace(request.ErrorMessage))
                    {
                        eventLog.MarkAsFailed(request.ErrorMessage);
                    }
                    else if (request.MarkAsProcessed)
                    {
                        eventLog.MarkAsProcessed();
                    }

                    _eventLogRepository.Update(eventLog);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
