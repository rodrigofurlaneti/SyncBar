using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByCompanyId
{
    public sealed record GetAllKeetaMerchantMappingsByCompanyIdQuery(
        long CompanyId) : IQuery<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>;
}
