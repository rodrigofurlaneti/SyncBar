using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create
{
    internal sealed class CreateKeetaIntegrationOrderEventLogCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationOrderEventLogCommand, CreateKeetaIntegrationOrderEventLogResponse>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationOrderEventLogCommandHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationOrderEventLogResponse>> Handle(
            CreateKeetaIntegrationOrderEventLogCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationOrderEventLogCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _eventLogRepository.ExistsByEventIdAsync(request.EventId, cancellationToken);
                    if (exists)
                    {
                        return Result.Failure<CreateKeetaIntegrationOrderEventLogResponse>(
                            Error.Conflict(
                                "KeetaOrderEventLog.AlreadyExists",
                                $"Já existe um evento Keeta registrado com o EventId {request.EventId}."));
                    }

                    var logResult = KeetaIntegrationOrderEventLog.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.EventId,
                        request.OrderId,
                        request.EventType,
                        request.RawPayload,
                        request.EventCreatedAtUtc);

                    if (logResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationOrderEventLogResponse>(logResult.Error);

                    var eventLog = logResult.Value;

                    await _eventLogRepository.AddAsync(eventLog, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationOrderEventLogResponse(
                        eventLog.Id, eventLog.CompanyId, eventLog.BranchId, eventLog.EventId, eventLog.OrderId, eventLog.EventType);

                    return Result.Success(response);
                });
        }
    }
}
