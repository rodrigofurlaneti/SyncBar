using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public sealed record AsaasTokenizeCreditCardResponse(
        [property: JsonPropertyName("creditCardToken")] string CreditCardToken,
        [property: JsonPropertyName("creditCardBrand")] string CreditCardBrand,
        [property: JsonPropertyName("creditCardNumber")] string CreditCardNumber);
}
