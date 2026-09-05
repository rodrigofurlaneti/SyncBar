using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs
{
    internal sealed class GetUnprocessedAsaasWebhookLogsQueryHandler
        : BaseQueryHandler<GetUnprocessedAsaasWebhookLogsQuery, IReadOnlyList<AsaasWebhookLogResponse>>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;

        public GetUnprocessedAsaasWebhookLogsQueryHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _webhookLogRepository = webhookLogRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasWebhookLogResponse>>> Handle(
            GetUnprocessedAsaasWebhookLogsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetUnprocessedAsaasWebhookLogsQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var logs = await _webhookLogRepository.GetUnprocessedLogsAsync(
                        request.CompanyId,
                        request.Limit,
                        cancellationToken);

                    var response = logs
                        .Select(log => new AsaasWebhookLogResponse(
                            log.Id,
                            log.CompanyId,
                            log.BranchId,
                            log.Event,
                            log.AsaasEventId,
                            log.PaymentId,
                            log.Payload,
                            log.RequestHeaders,
                            log.IpAddress,
                            log.Status.ToString(),
                            log.ErrorMessage,
                            log.CreatedAt,
                            log.ProcessedAt))
                        .ToList();

                    return Result.Success<IReadOnlyList<AsaasWebhookLogResponse>>(response);
                });
        }
    }
}
