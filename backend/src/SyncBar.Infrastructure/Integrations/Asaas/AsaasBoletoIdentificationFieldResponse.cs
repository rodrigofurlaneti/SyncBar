using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public record AsaasBoletoIdentificationFieldResponse(
        [property: JsonPropertyName("identificationField")] string IdentificationField, // Linha digitável
        [property: JsonPropertyName("barCode")] string? BarCode,
        [property: JsonPropertyName("nossoNumero")] string? NossoNumero
    );
}
