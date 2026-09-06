using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById
{
    internal sealed class GetKeetaOrderEventLogByIdQueryHandler
        : BaseQueryHandler<GetKeetaOrderEventLogByIdQuery, KeetaIntegrationOrderEventLogResponse>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;

        public GetKeetaOrderEventLogByIdQueryHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
        }

        public override async Task<Result<KeetaIntegrationOrderEventLogResponse>> Handle(
            GetKeetaOrderEventLogByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetKeetaOrderEventLogByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var eventLog = await _eventLogRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (eventLog is null)
                    {
                        return Result.Failure<KeetaIntegrationOrderEventLogResponse>(
                            Error.NotFound(
                                "KeetaOrderEventLog.NotFound",
                                $"Evento Keeta com ID {request.Id} não foi encontrado."));
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
