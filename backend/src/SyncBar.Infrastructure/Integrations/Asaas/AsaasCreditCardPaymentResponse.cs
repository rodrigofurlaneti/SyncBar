using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public sealed record AsaasCreditCardPaymentResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("value")] decimal Value,
        [property: JsonPropertyName("netValue")] decimal? NetValue,
        [property: JsonPropertyName("invoiceUrl")] string? InvoiceUrl);
}
