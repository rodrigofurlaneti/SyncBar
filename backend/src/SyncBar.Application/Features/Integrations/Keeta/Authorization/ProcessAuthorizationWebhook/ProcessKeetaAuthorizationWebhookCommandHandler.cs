using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook
{
    internal sealed class ProcessKeetaAuthorizationWebhookCommandHandler : BaseCommandHandler<ProcessKeetaAuthorizationWebhookCommand>
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IKeetaIntegrationAuthorizationSessionRepository _sessionRepository;
        private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository;
        private readonly IKeetaCredentialsResolver _credentialsResolver;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessKeetaAuthorizationWebhookCommandHandler(
            IKeetaIntegrationAuthorizationSessionRepository sessionRepository,
            IKeetaIntegrationMerchantMappingRepository mappingRepository,
            IKeetaCredentialsResolver credentialsResolver,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _mappingRepository = mappingRepository;
            _credentialsResolver = credentialsResolver;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(ProcessKeetaAuthorizationWebhookCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ProcessKeetaAuthorizationWebhookCommandHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    KeetaWebhookNotificationDto? payload;
                    try
                    {
                        payload = JsonSerializer.Deserialize<KeetaWebhookNotificationDto>(request.RawPayload, JsonOptions);
                    }
                    catch (JsonException)
                    {
                        return Result.Failure(new Error("Keeta.InvalidPayload", "Payload do webhook não é um JSON válido."));
                    }

                    if (payload is null || string.IsNullOrWhiteSpace(payload.AuthId))
                        return Result.Failure(new Error("Keeta.InvalidPayload", "Payload do webhook incompleto — authId ausente."));

                    var signatureError = await ValidateSignatureAsync(request.RawPayload, request.Signature, cancellationToken);
                    if (signatureError is not null)
                        return Result.Failure(signatureError);

                    var session = await _sessionRepository.GetByAuthIdAsync(payload.AuthId, cancellationToken);

                    if (payload.OpType == 2)
                    {
                        // Cancelamento de autorização: revoga o mapeamento independente de existir
                        // (ou não) uma sessão de autorização local para esse authId.
                        var mapping = await _mappingRepository.GetByKeetaMerchantIdAsync(payload.ShopId, cancellationToken);
                        if (mapping is not null)
                        {
                            mapping.UpdateStatus(isAuthorized: false, isOnboarded: mapping.IsOnboarded);
                            _mappingRepository.Update(mapping);
                        }

                        if (session is not null)
                        {
                            session.MarkAsProcessed();
                            _sessionRepository.Update(session);
                        }

                        await _unitOfWork.CommitAsync(cancellationToken);
                        return Result.Success();
                    }

                    // opType == 1 (nova autorização): se a sessão ainda não existe, o callback do
                    // navegador (que carrega o companyId/branchId reais) provavelmente ainda não
                    // chegou — nada a persistir aqui sem violar a FK de tenant; o próprio callback
                    // busca os dados de merchant de forma síncrona e não depende deste webhook.
                    if (session is null)
                        return Result.Success();

                    var existingMapping = await _mappingRepository.GetByKeetaMerchantIdAsync(payload.ShopId, cancellationToken);
                    if (existingMapping is null)
                    {
                        var mappingResult = KeetaIntegrationMerchantMapping.Create(
                            session.CompanyId, session.BranchId, payload.ShopId.ToString(), payload.ShopId, payload.ShopName);

                        if (mappingResult.IsSuccess)
                            await _mappingRepository.AddAsync(mappingResult.Value, cancellationToken);
                    }
                    else
                    {
                        existingMapping.UpdateStatus(isAuthorized: true, isOnboarded: existingMapping.IsOnboarded);
                        _mappingRepository.Update(existingMapping);
                    }

                    session.MarkAsProcessed();
                    _sessionRepository.Update(session);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }

        private async Task<Error?> ValidateSignatureAsync(string rawPayload, string? signature, CancellationToken cancellationToken)
        {
            var credentials = await _credentialsResolver.ResolveDefaultAsync(cancellationToken);

            // Sem client secret configurado no appsettings não há como validar — aceita o webhook
            // (ambiente ainda não configurado) em vez de bloquear toda a integração.
            if (string.IsNullOrWhiteSpace(credentials.ClientSecret))
                return null;

            if (string.IsNullOrWhiteSpace(signature))
                return Error.Validation("Keeta.MissingSignature", "Cabeçalho X-App-Signature ausente.");

            var keyBytes = Encoding.UTF8.GetBytes(credentials.ClientSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
            var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
            // A doc do POST /v1/newEvent especifica explicitamente "SHA256 hash ... encoded into
            // Base64 format" — o webhook de autorização descreve apenas "SHA256 hash of the
            // request body" sem citar a codificação, então seguimos a mesma convenção (Base64) por
            // consistência entre os dois webhooks da mesma API.
            var computedSignature = Convert.ToBase64String(hash);

            if (!string.Equals(computedSignature, signature.Trim(), StringComparison.Ordinal))
                return Error.Validation("Keeta.InvalidSignature", "Assinatura do webhook inválida.");

            return null;
        }

        private sealed record KeetaWebhookNotificationDto(
            [property: JsonPropertyName("clientId")] long ClientId,
            [property: JsonPropertyName("authId")] string AuthId,
            [property: JsonPropertyName("opType")] int OpType,
            [property: JsonPropertyName("shopId")] long ShopId,
            [property: JsonPropertyName("shopName")] string ShopName,
            [property: JsonPropertyName("createTime")] long CreateTime);
    }
}
