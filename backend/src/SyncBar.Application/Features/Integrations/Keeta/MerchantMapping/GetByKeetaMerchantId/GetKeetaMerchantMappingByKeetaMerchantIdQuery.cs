using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByKeetaMerchantId
{
    public sealed record GetKeetaMerchantMappingByKeetaMerchantIdQuery(
        long KeetaMerchantId) : IQuery<KeetaIntegrationMerchantMappingResponse>;
}
