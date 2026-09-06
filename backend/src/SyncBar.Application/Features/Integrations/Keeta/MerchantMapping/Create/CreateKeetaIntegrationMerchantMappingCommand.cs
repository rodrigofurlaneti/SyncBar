using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create
{
    public sealed record CreateKeetaIntegrationMerchantMappingCommand(
        long CompanyId,
        long BranchId,
        string InternalMerchantId,
        long KeetaMerchantId,
        string StoreName) : ICommand<CreateKeetaIntegrationMerchantMappingResponse>;
}
