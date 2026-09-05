using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate
{
    internal sealed class GetAsaasWebhookLogByIdForUpdateQueryHandler
        : BaseQueryHandler<GetAsaasWebhookLogByIdForUpdateQuery, AsaasWebhookLogResponse>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;

        public GetAsaasWebhookLogByIdForUpdateQueryHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _webhookLogRepository = webhookLogRepository;
        }

        public override async Task<Result<AsaasWebhookLogResponse>> Handle(
            GetAsaasWebhookLogByIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasWebhookLogByIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca o log com tracking habilitado para preparação de mutação/reprocessamento
                    var log = await _webhookLogRepository.GetByIdForUpdateAsync(
                        request.Id,
                        cancellationToken);

                    if (log is null || log.CompanyId != request.CompanyId)
                    {
                        return Result.Failure<AsaasWebhookLogResponse>(
                            Error.NotFound(
                                "AsaasWebhookLog.NotFound",
                                $"Log de webhook com ID {request.Id} não foi encontrado para atualização nesta empresa."));
                    }

                    var response = new AsaasWebhookLogResponse(
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
                        log.ProcessedAt);

                    return Result.Success(response);
                });
        }
    }
}
