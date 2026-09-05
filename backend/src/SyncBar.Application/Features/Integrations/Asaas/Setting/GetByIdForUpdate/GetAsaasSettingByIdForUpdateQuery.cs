using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByIdForUpdate
{
    public sealed record GetAsaasSettingByIdForUpdateQuery(
        long Id) : IQuery<AsaasIntegrationSettingResponse>;
}
