using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public record AsaasErrorDetail(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("description")] string Description
    );
}
