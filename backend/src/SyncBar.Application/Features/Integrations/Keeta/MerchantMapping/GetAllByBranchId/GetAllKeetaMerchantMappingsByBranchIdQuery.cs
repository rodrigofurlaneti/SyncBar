using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId
{
    public sealed record GetAllKeetaMerchantMappingsByBranchIdQuery(
        long BranchId) : IQuery<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>;
}
