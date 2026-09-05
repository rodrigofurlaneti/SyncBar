namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record AsaasPaymentResponse(
        string Id,
        string Status,
        decimal Value,
        decimal? NetValue,
        DateTime? PaymentDate,
        string? InvoiceUrl = null,
        string? BankSlipUrl = null);
}
