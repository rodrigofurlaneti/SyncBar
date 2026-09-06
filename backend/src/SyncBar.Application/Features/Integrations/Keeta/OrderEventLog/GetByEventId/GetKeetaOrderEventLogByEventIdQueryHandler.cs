using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId
{
    internal sealed class GetKeetaOrderEventLogByEventIdQueryHandler
        : BaseQueryHandler<GetKeetaOrderEventLogByEventIdQuery, KeetaIntegrationOrderEventLogResponse>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;

        public GetKeetaOrderEventLogByEventIdQueryHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
        }

        public override async Task<Result<KeetaIntegrationOrderEventLogResponse>> Handle(
            GetKeetaOrderEventLogByEventIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaOrderEventLogByEventIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var eventLog = await _eventLogRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                    if (eventLog is null)
                    {
                        return Result.Failure<KeetaIntegrationOrderEventLogResponse>(
                            Error.NotFound(
                                "KeetaOrderEventLog.NotFound",
                                $"Evento Keeta com EventId {request.EventId} não foi encontrado."));
                    }

                    var response = new KeetaIntegrationOrderEventLogResponse(
                        eventLog.Id,
                        eventLog.CompanyId,
                        eventLog.BranchId,
                        eventLog.EventId,
                        eventLog.OrderId,
                        eventLog.EventType,
                        eventLog.RawPayload,
                        eventLog.ProcessedSuccessfully,
                        eventLog.ErrorMessage,
                        eventLog.EventCreatedAtUtc,
                        eventLog.ReceivedAtUtc);

                    return Result.Success(response);
                });
        }
    }
}
