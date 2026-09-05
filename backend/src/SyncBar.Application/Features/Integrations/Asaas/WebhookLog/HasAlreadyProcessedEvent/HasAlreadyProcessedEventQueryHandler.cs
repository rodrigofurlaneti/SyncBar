using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent
{
    internal sealed class HasAlreadyProcessedEventQueryHandler
        : BaseQueryHandler<HasAlreadyProcessedEventQuery, bool>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;

        public HasAlreadyProcessedEventQueryHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _webhookLogRepository = webhookLogRepository;
        }

        public override async Task<Result<bool>> Handle(
            HasAlreadyProcessedEventQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(HasAlreadyProcessedEventQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Verifica se o evento já foi registrado ou processado para garantir idempotência
                    var hasProcessed = await _webhookLogRepository.HasAlreadyProcessedEventAsync(
                        request.AsaasEventId,
                        cancellationToken);

                    return Result.Success(hasProcessed);
                });
        }
    }
}
