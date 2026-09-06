using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId
{
    internal sealed class ExistsKeetaOrderEventLogByEventIdQueryHandler
        : BaseQueryHandler<ExistsKeetaOrderEventLogByEventIdQuery, bool>
    {
        private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository;

        public ExistsKeetaOrderEventLogByEventIdQueryHandler(
            IKeetaIntegrationOrderEventLogRepository eventLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _eventLogRepository = eventLogRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsKeetaOrderEventLogByEventIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsKeetaOrderEventLogByEventIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _eventLogRepository.ExistsByEventIdAsync(request.EventId, cancellationToken);
                    return Result.Success(exists);
                });
        }
    }
}
