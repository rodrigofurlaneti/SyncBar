using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Domain.Enums;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update
{
    internal sealed class UpdateAsaasWebhookLogStatusCommandHandler
        : BaseCommandHandler<UpdateAsaasWebhookLogStatusCommand>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAsaasWebhookLogStatusCommandHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _webhookLogRepository = webhookLogRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateAsaasWebhookLogStatusCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateAsaasWebhookLogStatusCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var log = await _webhookLogRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (log is null || log.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasWebhookLog.NotFound",
                                $"Log de webhook com ID {request.Id} não foi encontrado para esta empresa."));
                    }

                    // Aplica a transição de estado no agregado de domínio
                    var updateResult = request.Status switch
                    {
                        WebhookLogStatus.Processed => log.MarkAsProcessed(),
                        WebhookLogStatus.Failed => log.MarkAsFailed(request.ErrorMessage ?? "Erro desconhecido durante o processamento do webhook."),
                        _ => Result.Failure(Error.Validation("WebhookLog.InvalidStatus", $"Transição para o status {request.Status} não é permitida."))
                    };

                    if (updateResult.IsFailure)
                        return Result.Failure(updateResult.Error);

                    _webhookLogRepository.Update(log);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
