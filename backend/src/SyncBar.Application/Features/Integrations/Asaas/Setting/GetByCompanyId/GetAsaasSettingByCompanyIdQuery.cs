using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId
{
    public sealed record GetAsaasSettingByCompanyIdQuery(
        long CompanyId) : IQuery<AsaasIntegrationSettingResponse>;
}
