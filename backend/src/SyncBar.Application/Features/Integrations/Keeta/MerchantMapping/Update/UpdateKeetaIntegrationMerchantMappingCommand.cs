using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Update
{
    public sealed record UpdateKeetaIntegrationMerchantMappingCommand(
        long Id,
        long CompanyId,
        bool? IsAuthorized = null,
        bool? IsOnboarded = null,
        string? MenuBaseUrl = null,
        string? WebhookUrl = null,
        bool RegisterMenuSync = false) : ICommand;
}
