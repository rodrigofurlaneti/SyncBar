namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId
{
    public sealed record AsaasIntegrationPaymentResponse(
        long Id,
        long BranchId,
        long CustomerOrderId,
        long? CustomerId,
        string AsaasPaymentId,
        string BillingType,
        string Status,
        decimal Value,
        decimal? NetValue,
        DateTime DueDate,
        DateTime? PaymentDate,
        string? PixQrCodeBase64,
        string? PixPayload,
        string? InvoiceUrl,
        string? BankSlipUrl,
        int InstallmentCount,
        string? CreditCardToken,
        DateTime CreatedAt,
        bool IsActive);
}
