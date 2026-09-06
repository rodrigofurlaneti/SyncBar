using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Polling
{
    internal sealed class PollKeetaEventsCommandHandler : BaseCommandHandler<PollKeetaEventsCommand, PollKeetaEventsResponse>
    {
        private readonly IKeetaOrderClient _orderClient;
        private readonly IKeetaOrderEventProcessor _eventProcessor;
        private readonly IUnitOfWork _unitOfWork;

        public PollKeetaEventsCommandHandler(
            IKeetaOrderClient orderClient,
            IKeetaOrderEventProcessor eventProcessor,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderClient = orderClient;
            _eventProcessor = eventProcessor;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<PollKeetaEventsResponse>> Handle(PollKeetaEventsCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(PollKeetaEventsCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var events = await _orderClient.PollEventsAsync(request.CompanyId, request.BranchId, request.MerchantIds, cancellationToken);

                    if (events.Count == 0)
                        return Result.Success(new PollKeetaEventsResponse(0, 0, 0));

                    var processed = 0;
                    var unprocessed = 0;

                    foreach (var polledEvent in events)
                    {
                        var wasProcessed = await _eventProcessor.ProcessAsync(request.CompanyId, request.BranchId, polledEvent, cancellationToken);
                        if (wasProcessed) processed++; else unprocessed++;
                    }

                    // Confirma TODOS os eventos polled (processados ou não) — eventos não
                    // reconhecidos continuam sendo reenviados pela Keeta nos próximos ciclos, e
                    // como já ficaram persistidos no log pelo processor acima, não há perda de
                    // dados em deixar de reprocessá-los aqui.
                    await _orderClient.AcknowledgeEventsAsync(request.CompanyId, request.BranchId, events, cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(new PollKeetaEventsResponse(events.Count, processed, unprocessed));
                });
        }
    }
}
