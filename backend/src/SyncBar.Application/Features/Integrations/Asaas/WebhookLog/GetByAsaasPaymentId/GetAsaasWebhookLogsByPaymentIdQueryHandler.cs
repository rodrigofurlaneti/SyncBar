using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId
{
    internal sealed class GetAsaasWebhookLogsByPaymentIdQueryHandler
        : BaseQueryHandler<GetAsaasWebhookLogsByPaymentIdQuery, IReadOnlyList<AsaasWebhookLogResponse>>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;

        public GetAsaasWebhookLogsByPaymentIdQueryHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _webhookLogRepository = webhookLogRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasWebhookLogResponse>>> Handle(
            GetAsaasWebhookLogsByPaymentIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasWebhookLogsByPaymentIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var logs = await _webhookLogRepository.GetByPaymentIdAsync(
                        request.CompanyId,
                        request.PaymentId,
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
