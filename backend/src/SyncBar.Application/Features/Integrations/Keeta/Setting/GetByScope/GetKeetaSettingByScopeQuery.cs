using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope
{
    public sealed record GetKeetaSettingByScopeQuery(
        long CompanyId,
        long? BranchId) : IQuery<KeetaIntegrationSettingResponse>;
}
