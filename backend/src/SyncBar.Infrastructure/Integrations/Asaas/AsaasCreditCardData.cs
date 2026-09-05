using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public record AsaasCreditCardData(
            [property: JsonPropertyName("creditCardNumber")] string CreditCardNumber,
            [property: JsonPropertyName("creditCardBrand")] string CreditCardBrand,
            [property: JsonPropertyName("creditCardToken")] string? CreditCardToken // Token para futuras compras
        );
}
