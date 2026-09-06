using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById
{
    public sealed record GetKeetaMerchantMappingByIdQuery(
        long Id) : IQuery<KeetaIntegrationMerchantMappingResponse>;
}
