using System.Text.Json;
using System.Text.Json.Serialization;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive
{
    internal sealed class ReceiveAsaasWebhookCommandHandler : BaseCommandHandler<ReceiveAsaasWebhookCommand>
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;
        private readonly IAsaasIntegrationSettingRepository _settingRepository;
        private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReceiveAsaasWebhookCommandHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            IAsaasIntegrationSettingRepository settingRepository,
            IAsaasIntegrationWebhookLogRepository webhookLogRepository,
            IBranchRepository branchRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _settingRepository = settingRepository;
            _webhookLogRepository = webhookLogRepository;
            _branchRepository = branchRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(ReceiveAsaasWebhookCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ReceiveAsaasWebhookCommandHandler),
                nameof(Handle),
                request.IpAddress,
                async (userIdBox) =>
                {
                    AsaasWebhookPayloadDto? payload;
                    try
                    {
                        payload = JsonSerializer.Deserialize<AsaasWebhookPayloadDto>(request.RawPayload, JsonOptions);
                    }
                    catch (JsonException)
                    {
                        return Result.Failure(new Error("Asaas.InvalidPayload", "Payload do webhook não é um JSON válido."));
                    }

                    if (payload is null || string.IsNullOrWhiteSpace(payload.Event) || payload.Payment is null
                        || string.IsNullOrWhiteSpace(payload.Payment.Id))
                    {
                        return Result.Failure(new Error("Asaas.InvalidPayload", "Payload do webhook incompleto — evento ou cobrança ausente."));
                    }

                    if (!payload.Event.StartsWith("PAYMENT_", StringComparison.OrdinalIgnoreCase))
                        return Result.Success();

                    var payment = await _paymentRepository.GetByAsaasPaymentIdForUpdateAsync(payload.Payment.Id, cancellationToken);
                    if (payment is null)
                    {
                        return Result.Success();
                    }

                    var branch = await _branchRepository.GetByIdAsync(payment.BranchId, cancellationToken);
                    if (branch is not null)
                    {
                        var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(
                            branch.CompanyId, payment.BranchId, cancellationToken);

                        if (setting is not null && !string.IsNullOrWhiteSpace(setting.WebhookSecretEncrypted)
                            && !string.Equals(setting.WebhookSecretEncrypted, request.AccessToken, StringComparison.Ordinal))
                        {
                            return Result.Failure(new Error(
                                "Asaas.InvalidWebhookToken", "Token de autenticação do webhook inválido para esta unidade."));
                        }

                        await RegisterAuditLogAsync(branch.CompanyId, payment.BranchId, payload, request, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(payload.Payment.Status))
                    {
                        payment.UpdateStatus(
                            payload.Payment.Status,
                            payload.Payment.NetValue,
                            ParseAsaasDate(payload.Payment.PaymentDate));
                    }

                    if (!string.IsNullOrWhiteSpace(payload.Payment.InvoiceUrl) || !string.IsNullOrWhiteSpace(payload.Payment.BankSlipUrl))
                    {
                        payment.SetUrls(payload.Payment.InvoiceUrl, payload.Payment.BankSlipUrl);
                    }

                    _paymentRepository.Update(payment);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }

        private async Task RegisterAuditLogAsync(
            long companyId,
            long branchId,
            AsaasWebhookPayloadDto payload,
            ReceiveAsaasWebhookCommand request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(payload.Id) && await _webhookLogRepository.ExistsByEventIdAsync(payload.Id, cancellationToken))
                return;

            var logResult = AsaasIntegrationWebhookLog.Create(
                companyId, branchId, payload.Event!, payload.Id, payload.Payment!.Id, request.RawPayload, null, request.IpAddress);

            if (logResult.IsFailure)
                return;

            var log = logResult.Value;
            log.MarkAsProcessed();
            await _webhookLogRepository.AddAsync(log, cancellationToken);
        }

        private static DateTime? ParseAsaasDate(string? value)
            => !string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var parsed) ? parsed : null;

        private sealed record AsaasWebhookPayloadDto(
            string? Id,
            string? Event,
            [property: JsonPropertyName("payment")] AsaasWebhookPaymentDto? Payment);

        private sealed record AsaasWebhookPaymentDto(
            string? Id,
            decimal? Value,
            decimal? NetValue,
            string? BillingType,
            string? Status,
            string? DueDate,
            string? PaymentDate,
            string? InvoiceUrl,
            string? BankSlipUrl);
    }
}
