using SyncBar.Domain.Enums;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AsaasIntegrationWebhookLog : AggregateRoot
{
    public long CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public string Event { get; private set; } = string.Empty;
    public string? AsaasEventId { get; private set; }
    public string? PaymentId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string? RequestHeaders { get; private set; }
    public string? IpAddress { get; private set; }
    public WebhookLogStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AsaasIntegrationWebhookLog() : base(0) { }

    private AsaasIntegrationWebhookLog(
        long companyId,
        long? branchId,
        string @event,
        string? asaasEventId,
        string? paymentId,
        string payload,
        string? requestHeaders,
        string? ipAddress) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        Event = @event;
        AsaasEventId = asaasEventId;
        PaymentId = paymentId;
        Payload = payload;
        RequestHeaders = requestHeaders;
        IpAddress = ipAddress;
        Status = WebhookLogStatus.Pending;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AsaasIntegrationWebhookLog> Create(
        long companyId,
        long? branchId,
        string @event,
        string? asaasEventId,
        string? paymentId,
        string payload,
        string? requestHeaders = null,
        string? ipAddress = null)
    {
        if (companyId <= 0)
            return Result.Failure<AsaasIntegrationWebhookLog>(
                new Error("CompanyId.Invalid", "CompanyId inválido."));

        if (branchId.HasValue && branchId.Value <= 0)
            return Result.Failure<AsaasIntegrationWebhookLog>(
                new Error("BranchId.Invalid", "BranchId inválido."));

        if (string.IsNullOrWhiteSpace(@event))
            return Result.Failure<AsaasIntegrationWebhookLog>(
                new Error("Event.Empty", "O tipo de evento é obrigatório."));

        if (string.IsNullOrWhiteSpace(payload))
            return Result.Failure<AsaasIntegrationWebhookLog>(
                new Error("Payload.Empty", "O payload JSON é obrigatório."));

        return Result.Success(new AsaasIntegrationWebhookLog(
            companyId,
            branchId,
            @event,
            asaasEventId,
            paymentId,
            payload,
            requestHeaders,
            ipAddress));
    }

    public Result MarkAsProcessed()
    {
        if (Status == WebhookLogStatus.Processed)
            return Result.Failure(new Error("WebhookLog.AlreadyProcessed", "Este log de webhook já foi processado."));

        Status = WebhookLogStatus.Processed;
        ErrorMessage = null;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure(new Error("ErrorMessage.Empty", "A mensagem de erro é obrigatória ao marcar como falha."));

        Status = WebhookLogStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}