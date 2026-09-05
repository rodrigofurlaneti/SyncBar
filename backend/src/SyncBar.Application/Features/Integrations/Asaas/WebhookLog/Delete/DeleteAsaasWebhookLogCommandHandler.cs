using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete
{
    internal sealed class DeleteAsaasWebhookLogCommandHandler
        : BaseCommandHandler<DeleteAsaasWebhookLogCommand>
    {
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAsaasWebhookLogCommandHandler(
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _webhookLogRepository = webhookLogRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteAsaasWebhookLogCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteAsaasWebhookLogCommandHandler),
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

                    _webhookLogRepository.Delete(log);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
