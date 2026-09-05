using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public record AsaasPixQrCodeResponse(
        [property: JsonPropertyName("encodedImage")] string EncodedImage, // Base64 do PNG
        [property: JsonPropertyName("payload")] string Payload,           // Código copia e cola
        [property: JsonPropertyName("expirationDate")] DateTime ExpirationDate
    );
}
