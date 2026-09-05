using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public sealed record AsaasErrorItem(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("description")] string Description);
}
