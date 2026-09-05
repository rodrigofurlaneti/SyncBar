using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchId
{
    public sealed record GetAsaasSettingByBranchIdQuery(
        long CompanyId,
        long BranchId) : IQuery<AsaasIntegrationSettingResponse>;
}
