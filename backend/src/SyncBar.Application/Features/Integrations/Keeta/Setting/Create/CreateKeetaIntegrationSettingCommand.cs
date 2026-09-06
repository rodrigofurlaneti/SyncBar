using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.Create
{
    public sealed record CreateKeetaIntegrationSettingCommand(
        long CompanyId,
        long BranchId,
        string ClientId,
        string ClientSecret,
        string AppId,
        string? BaseUrl = null) : ICommand<CreateKeetaIntegrationSettingResponse>;
}
