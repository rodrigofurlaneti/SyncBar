using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.Delete
{
    public sealed record DeleteAsaasIntegrationSettingCommand(
        long Id,
        long CompanyId) : ICommand;
}
