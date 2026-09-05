using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive
{
    public sealed record GetAllActiveAsaasSettingsQuery(
        long CompanyId) : IQuery<IReadOnlyList<AsaasIntegrationSettingResponse>>;
}
