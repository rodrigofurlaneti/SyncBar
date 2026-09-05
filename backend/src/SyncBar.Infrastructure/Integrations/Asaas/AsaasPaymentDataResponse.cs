namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public sealed record AsaasPaymentDataResponse(
        string Id,
        string Customer,
        string Status,
        decimal Value,
        decimal? NetValue,
        string BillingType,
        string? InvoiceUrl,
        string? BankSlipUrl);
}
