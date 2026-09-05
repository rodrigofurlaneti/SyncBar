using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.Update
{
    public sealed record UpdateAsaasIntegrationSettingCommand(
        long Id,
        long CompanyId,
        string? ApiKey = null,
        string? WebhookToken = null,
        string? Environment = null,
        bool? IsActive = null) : ICommand;
}
