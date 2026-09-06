using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchIdForUpdate
{
    public sealed record GetKeetaSettingByBranchIdForUpdateQuery(
        long BranchId) : IQuery<KeetaIntegrationSettingResponse>;
}
