using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete
{
    public sealed record DeleteKeetaIntegrationMerchantMappingCommand(
        long Id,
        long CompanyId) : ICommand;
}
