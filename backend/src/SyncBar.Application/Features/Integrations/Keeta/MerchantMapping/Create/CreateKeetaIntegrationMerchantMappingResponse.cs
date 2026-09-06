namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create
{
    public sealed record CreateKeetaIntegrationMerchantMappingResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string InternalMerchantId,
        long KeetaMerchantId,
        string StoreName);
}
