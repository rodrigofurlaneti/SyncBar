using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Polling
{
    internal sealed class ProcessKeetaNewEventWebhookCommandHandler : BaseCommandHandler<ProcessKeetaNewEventWebhookCommand>
    {
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IKeetaCredentialsResolver _credentialsResolver;
        private readonly IKeetaOrderEventProcessor _eventProcessor;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessKeetaNewEventWebhookCommandHandler(
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            IKeetaCredentialsResolver credentialsResolver,
            IKeetaOrderEventProcessor eventProcessor,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _mappingRepository = mappingRepository;
            _credentialsResolver = credentialsResolver;
            _eventProcessor = eventProcessor;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(ProcessKeetaNewEventWebhookCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ProcessKeetaNewEventWebhookCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    if (!request.KeetaMerchantId.HasValue)
                        return Result.Failure(new Error("Keeta.InvalidPayload", "Cabeçalho X-App-MerchantId ausente."));

                    // O X-App-MerchantId do webhook é o único dado que permite correlacionar este
                    // evento (que não carrega companyId/branchId) com um tenant local — diferente
                    // do webhook de autorização (que depende do authId ainda não conhecido), aqui a
                    // loja já está mapeada desde a Fase 2/3.
                    var mapping = await _mappingRepository.GetByKeetaMerchantIdAsync(request.KeetaMerchantId.Value, cancellationToken);
                    if (mapping is null)
                        return Result.Failure(new Error("Keeta.UnknownMerchant", "Nenhum merchant mapeado para este X-App-MerchantId."));

                    var signatureError = await ValidateSignatureAsync(mapping.CompanyId, mapping.BranchId, request.RawPayload, request.Signature, cancellationToken);
                    if (signatureError is not null)
                        return Result.Failure(signatureError);

                    JsonElement element;
                    try
                    {
                        element = JsonDocument.Parse(request.RawPayload).RootElement;
                    }
                    catch (JsonException)
                    {
                        return Result.Failure(new Error("Keeta.InvalidPayload", "Payload do webhook não é um JSON válido."));
                    }

                    if (!element.TryGetProperty("eventId", out var eventIdProp)
                        || !element.TryGetProperty("eventType", out var eventTypeProp)
                        || !element.TryGetProperty("orderId", out var orderIdProp))
                    {
                        return Result.Failure(new Error("Keeta.InvalidPayload", "Payload do webhook incompleto — eventId/eventType/orderId ausentes."));
                    }

                    var createdAt = element.TryGetProperty("createdAt", out var createdAtProp) && createdAtProp.TryGetDateTime(out var parsed)
                        ? parsed
                        : DateTime.UtcNow;
                    var orderUrl = element.TryGetProperty("orderURL", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty;

                    var polledEvent = new KeetaPolledEvent(
                        eventIdProp.GetString() ?? string.Empty,
                        eventTypeProp.GetString() ?? string.Empty,
                        orderIdProp.GetString() ?? string.Empty,
                        orderUrl,
                        createdAt,
                        request.RawPayload);

                    await _eventProcessor.ProcessAsync(mapping.CompanyId, mapping.BranchId, polledEvent, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }

        private async Task<Error?> ValidateSignatureAsync(long companyId, long branchId, string rawPayload, string? signature, CancellationToken cancellationToken)
        {
            var credentials = await _credentialsResolver.ResolveAsync(companyId, branchId, cancellationToken);

            if (string.IsNullOrWhiteSpace(credentials.ClientSecret))
                return null;

            if (string.IsNullOrWhiteSpace(signature))
                return Error.Validation("Keeta.MissingSignature", "Cabeçalho X-App-Signature ausente.");

            var keyBytes = Encoding.UTF8.GetBytes(credentials.ClientSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
            var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
            var computedSignature = Convert.ToBase64String(hash);

            if (!string.Equals(computedSignature, signature.Trim(), StringComparison.Ordinal))
                return Error.Validation("Keeta.InvalidSignature", "Assinatura do webhook inválida.");

            return null;
        }
    }
}
