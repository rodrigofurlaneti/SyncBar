using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback
{
    public sealed record GetKeetaSettingByBranchOrCompanyFallbackQuery(
        long CompanyId,
        long? BranchId) : IQuery<KeetaIntegrationSettingResponse>;
}
