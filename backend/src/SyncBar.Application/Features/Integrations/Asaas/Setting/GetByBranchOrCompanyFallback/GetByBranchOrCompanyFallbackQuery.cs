using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback
{
    public sealed record GetByBranchOrCompanyFallbackQuery(
        long CompanyId,
        long? BranchId = null) : IQuery<AsaasIntegrationSettingResponse>;
}
