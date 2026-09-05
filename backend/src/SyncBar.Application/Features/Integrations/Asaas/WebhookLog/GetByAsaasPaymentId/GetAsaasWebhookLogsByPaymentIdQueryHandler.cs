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
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
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
                    // 1. Faz uma única consulta já filtrada e otimizada no banco de dados
                    var logs = await _webhookLogRepository.GetByPaymentIdAsync(
                        request.CompanyId,
                        request.PaymentId,
                        cancellationToken);

                    // 2. Valida se a consulta não retornou nenhum registro
                    if (logs == null || !logs.Any())
                    {
                        return Result.Failure<IReadOnlyList<AsaasWebhookLogResponse>>(
                            new Error(
                                "AsaasWebhookLogNotFound",
                                $"No webhook logs found for company ID {request.CompanyId} and payment ID {request.PaymentId}."));
                    }

                    // 3. Mapeia o resultado
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