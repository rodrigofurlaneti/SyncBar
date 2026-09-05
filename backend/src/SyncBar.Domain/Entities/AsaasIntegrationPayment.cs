using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AsaasIntegrationPayment : AggregateRoot
{
    public long BranchId { get; private set; }
    public long CustomerOrderId { get; private set; }
    public long? CustomerId { get; private set; }
    public string AsaasPaymentId { get; private set; } = string.Empty;
    public string BillingType { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public decimal Value { get; private set; }
    public decimal? NetValue { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public string? PixQrCodeBase64 { get; private set; }
    public string? PixPayload { get; private set; }
    public string? InvoiceUrl { get; private set; }
    public string? BankSlipUrl { get; private set; }
    public int InstallmentCount { get; private set; }
    public string? CreditCardToken { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AsaasIntegrationPayment() : base(0) { }

    private AsaasIntegrationPayment(
        long branchId,
        long customerOrderId,
        long? customerId,
        string asaasPaymentId,
        string billingType,
        decimal value,
        DateTime dueDate,
        int installmentCount = 1) : base(0)
    {
        BranchId = branchId;
        CustomerOrderId = customerOrderId;
        CustomerId = customerId;
        AsaasPaymentId = asaasPaymentId;
        BillingType = billingType;
        Value = value;
        DueDate = dueDate;
        InstallmentCount = installmentCount > 0 ? installmentCount : 1;
        Status = "PENDING";
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AsaasIntegrationPayment> Create(
        long branchId,
        long customerOrderId,
        long? customerId,
        string asaasPaymentId,
        string billingType,
        decimal value,
        DateTime dueDate,
        int installmentCount = 1)
    {
        if (branchId <= 0)
            return Result.Failure<AsaasIntegrationPayment>(
                new Error("BranchId.Invalid", "BranchId inválido."));

        if (customerOrderId <= 0)
            return Result.Failure<AsaasIntegrationPayment>(
                new Error("CustomerOrderId.Invalid", "CustomerOrderId inválido."));

        if (string.IsNullOrWhiteSpace(asaasPaymentId))
            return Result.Failure<AsaasIntegrationPayment>(
                new Error("AsaasPaymentId.Empty", "Identificador do Asaas é obrigatório."));

        if (value <= 0)
            return Result.Failure<AsaasIntegrationPayment>(
                new Error("Value.Invalid", "O valor deve ser maior que zero."));

        return Result.Success(new AsaasIntegrationPayment(
            branchId,
            customerOrderId,
            customerId,
            asaasPaymentId,
            billingType,
            value,
            dueDate,
            installmentCount));
    }

    public void SetPixDetails(string qrCodeBase64, string payload)
    {
        PixQrCodeBase64 = qrCodeBase64;
        PixPayload = payload;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetUrls(string? invoiceUrl, string? bankSlipUrl)
    {
        InvoiceUrl = invoiceUrl;
        BankSlipUrl = bankSlipUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCreditCardToken(string token)
    {
        CreditCardToken = token;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(decimal? netValue, DateTime? paymentDate = null)
    {
        Status = "RECEIVED";
        NetValue = netValue;
        PaymentDate = paymentDate ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string status,
        decimal? netValue = null,
        DateTime? paymentDate = null,
        string? pixQrCodeBase64 = null,
        string? pixPayload = null,
        string? invoiceUrl = null,
        string? bankSlipUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(status))
            Status = status;

        if (netValue.HasValue)
            NetValue = netValue.Value;

        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value;

        if (!string.IsNullOrWhiteSpace(pixQrCodeBase64))
            PixQrCodeBase64 = pixQrCodeBase64;

        if (!string.IsNullOrWhiteSpace(pixPayload))
            PixPayload = pixPayload;

        if (!string.IsNullOrWhiteSpace(invoiceUrl))
            InvoiceUrl = invoiceUrl;

        if (!string.IsNullOrWhiteSpace(bankSlipUrl))
            BankSlipUrl = bankSlipUrl;

        UpdatedAt = DateTime.UtcNow;
    }
    public void UpdateStatus(string status, decimal? netValue = null, DateTime? paymentDate = null)
    {
        if (!string.IsNullOrWhiteSpace(status))
            Status = status;

        if (netValue.HasValue)
            NetValue = netValue.Value;

        if (paymentDate.HasValue)
            PaymentDate = paymentDate.Value;

        UpdatedAt = DateTime.UtcNow;
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}