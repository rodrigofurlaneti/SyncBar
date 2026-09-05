using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchIdForUpdate
{
    public sealed record GetAsaasSettingByBranchIdForUpdateQuery(
        long CompanyId,
        long BranchId) : IQuery<AsaasIntegrationSettingResponse>;
}
