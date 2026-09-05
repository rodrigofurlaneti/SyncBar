using System.Text.Json.Serialization;
namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public sealed record AsaasErrorWrapper(
        [property: JsonPropertyName("errors")] List<AsaasErrorItem>? Errors);
}
