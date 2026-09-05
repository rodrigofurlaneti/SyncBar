using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create;

internal sealed class CreateAsaasWebhookLogCommandHandler
    : BaseCommandHandler<CreateAsaasWebhookLogCommand, CreateAsaasWebhookLogResponse>
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAsaasWebhookLogCommandHandler(
        IAsaasIntegrationWebhookLogRepository webhookLogRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _webhookLogRepository = webhookLogRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<CreateAsaasWebhookLogResponse>> Handle(
        CreateAsaasWebhookLogCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateAsaasWebhookLogCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                // 1. Verificação de idempotência para evitar duplicidade de evento
                if (!string.IsNullOrWhiteSpace(request.AsaasEventId))
                {
                    var isDuplicate = await _webhookLogRepository.ExistsByEventIdAsync(
                        request.AsaasEventId,
                        cancellationToken);

                    if (isDuplicate)
                    {
                        return Result.Failure<CreateAsaasWebhookLogResponse>(
                            new Error(
                                "AsaasWebhookLog.DuplicateEvent",
                                $"O evento do Asaas com ID '{request.AsaasEventId}' já foi recebido e processado."));
                    }
                }

                // 2. Instanciação via método de fábrica do AggregateRoot
                var logResult = AsaasIntegrationWebhookLog.Create(
                    request.CompanyId,
                    request.BranchId,
                    request.Event,
                    request.AsaasEventId,
                    request.PaymentId,
                    request.Payload,
                    request.RequestHeaders,
                    request.IpAddress);

                if (logResult.IsFailure)
                {
                    return Result.Failure<CreateAsaasWebhookLogResponse>(logResult.Error);
                }

                var webhookLog = logResult.Value;

                // 3. Persistência
                await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                var response = new CreateAsaasWebhookLogResponse(
                    webhookLog.Id,
                    webhookLog.Event,
                    webhookLog.PaymentId,
                    webhookLog.CreatedAt);

                return Result.Success(response);
            });
    }
}