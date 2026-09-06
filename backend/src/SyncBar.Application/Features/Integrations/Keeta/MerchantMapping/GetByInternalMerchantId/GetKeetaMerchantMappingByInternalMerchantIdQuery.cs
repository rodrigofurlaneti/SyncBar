using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId
{
    public sealed record GetKeetaMerchantMappingByInternalMerchantIdQuery(
        string InternalMerchantId) : IQuery<KeetaIntegrationMerchantMappingResponse>;
}
