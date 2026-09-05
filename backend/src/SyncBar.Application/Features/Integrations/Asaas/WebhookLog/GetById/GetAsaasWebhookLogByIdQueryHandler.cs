using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById
{
    internal sealed class GetAsaasWebhookLogByIdQueryHandler
        : BaseQueryHandler<GetAsaasWebhookLogByIdQuery, AsaasWebhookLogResponse>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;

        public GetAsaasWebhookLogByIdQueryHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _webhookLogRepository = webhookLogRepository;
        }

        public override async Task<Result<AsaasWebhookLogResponse>> Handle(
            GetAsaasWebhookLogByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasWebhookLogByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var log = await _webhookLogRepository.GetByIdAsync(
                        request.Id,
                        cancellationToken);

                    if (log is null || log.CompanyId != request.CompanyId)
                    {
                        return Result.Failure<AsaasWebhookLogResponse>(
                            Error.NotFound(
                                "AsaasWebhookLog.NotFound",
                                $"Log de webhook com ID {request.Id} não foi encontrado para esta empresa."));
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
