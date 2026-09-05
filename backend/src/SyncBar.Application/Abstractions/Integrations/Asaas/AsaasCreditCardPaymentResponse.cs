namespace SyncBar.Application.Abstractions.Integrations.Asaas
{
    public sealed record AsaasCreditCardPaymentResponse(string Id, string Status, decimal Value, decimal? NetValue, string? CreditCardToken);
}
