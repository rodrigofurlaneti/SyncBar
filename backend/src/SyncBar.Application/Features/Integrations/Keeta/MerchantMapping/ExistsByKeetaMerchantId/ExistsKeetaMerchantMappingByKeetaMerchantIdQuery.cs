using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId
{
    public sealed record ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(
        long KeetaMerchantId) : IQuery<bool>;
}
