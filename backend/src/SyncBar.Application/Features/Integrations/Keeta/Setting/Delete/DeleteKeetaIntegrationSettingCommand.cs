using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Delete
{
    public sealed record DeleteKeetaIntegrationSettingCommand(
        long Id,
        long CompanyId) : ICommand;
}
