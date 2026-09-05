using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyIdForUpdate
{
    public sealed record GetAsaasSettingByCompanyIdForUpdateQuery(
        long CompanyId) : IQuery<AsaasIntegrationSettingResponse>;
}
